using System.Diagnostics;
using Ionic.Zip;
using IonicCompressionLevel = Ionic.Zlib.CompressionLevel;

namespace RimWorldBackup;

public record ProgressReport(string Message, int Percentage);

public class BackupOptions
{
    public bool Config { get; set; } = true;
    public bool Saves { get; set; } = true;
    public bool WorkshopMods { get; set; } = true;
    public bool LocalMods { get; set; } = true;
    public bool GameInfo { get; set; } = true;
    public bool GameFiles { get; set; } = true;
    public bool PlayerLog { get; set; } = true;
}

public class BackupEntry
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public long SizeBytes { get; init; }
    public string DisplayName => $"{Name} - {Helpers.FormatSize(SizeBytes)}";
}

public static class Helpers
{
    public static string FormatSize(long bytes) => bytes switch
    {
        > 1073741824L => $"{bytes / 1073741824.0:N1} GB",
        > 1048576L => $"{bytes / 1048576.0:N1} MB",
        > 1024L => $"{bytes / 1024.0:N1} KB",
        _ => $"{bytes} B"
    };
}

public class BackupManager
{
    private readonly string _backupDir;
    public string BackupDir => _backupDir;
    public BackupManager(string rootDir) { _backupDir = Path.Combine(rootDir, "RimWorld_Backup"); }

    public static string? FindSteamPath()
    {
        try { using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"); if (key?.GetValue("SteamPath") is string sp && IsValidSteamPath(sp)) return sp; } catch { }
        foreach (var p in new[] { @"E:\SteamLibrary", @"D:\SteamLibrary", @"C:\Program Files (x86)\Steam", @"C:\Program Files\Steam" }) { if (Directory.Exists(Path.Combine(p, @"steamapps\common\RimWorld"))) return p; }
        return null;
    }
    public static bool IsValidSteamPath(string path) => !string.IsNullOrWhiteSpace(path) && Directory.Exists(Path.Combine(path.TrimEnd('\\'), @"steamapps\common\RimWorld"));

    public static bool IsGameRunning() =>
        Process.GetProcessesByName("RimWorldWin64").Any() ||
        Process.GetProcessesByName("RimWorld").Any();

    public List<BackupEntry> GetBackupZips()
    {
        if (!Directory.Exists(_backupDir)) return new();
        return Directory.GetFiles(_backupDir, "*.zip").Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).Select(f => new BackupEntry { Path = f.FullName, Name = Path.GetFileNameWithoutExtension(f.Name), SizeBytes = f.Length }).ToList();
    }

    public async Task BackupAsync(string steamPath, BackupOptions options, string backupName, IProgress<ProgressReport> progress, CancellationToken ct)
    {
        if (IsGameRunning())
            throw new InvalidOperationException("RimWorld is running. Please close the game before backing up.");

        var rw = Path.Combine(steamPath, @"steamapps\common\RimWorld");
        var ws = Path.Combine(steamPath, @"steamapps\workshop\content\294100");
        var ad = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", @"Ludeon Studios\RimWorld by Ludeon Studios");
        var tmp = Path.Combine(Path.GetTempPath(), $"RWBackup_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(tmp);
        try
        {
            int total = 0;
            if (options.Config) total++; if (options.Saves) total++; if (options.WorkshopMods) total++; if (options.LocalMods) total++;
            if (options.GameInfo) total++; if (options.PlayerLog) total++; if (options.GameFiles) total++; if (total == 0) total = 1;
            var r = new StepReporter(progress, total);
            if (options.Config) { r.Report("Config...", ct); await Task.Run(() => CopyConfig(ad, tmp, ct), ct); r.StepDone(); ct.ThrowIfCancellationRequested(); }
            if (options.Saves) { r.Report("Saves...", ct); await Task.Run(() => CopySaves(ad, tmp, ct), ct); r.StepDone(); ct.ThrowIfCancellationRequested(); }
            if (options.WorkshopMods) { r.Report("Workshop...", ct); await Task.Run(() => CopyWorkshopMods(ws, tmp, r, ct), ct); r.StepDone(); ct.ThrowIfCancellationRequested(); }
            if (options.LocalMods) { r.Report("Local mods...", ct); await Task.Run(() => CopyLocalMods(rw, tmp, r, ct), ct); r.StepDone(); ct.ThrowIfCancellationRequested(); }
            if (options.GameInfo) { r.Report("Game info...", ct); await Task.Run(() => CopyGameInfo(rw, tmp), ct); r.StepDone(); ct.ThrowIfCancellationRequested(); }
            if (options.PlayerLog) { r.Report("Player.log...", ct); await Task.Run(() => CopyPlayerLog(ad, tmp), ct); r.StepDone(); ct.ThrowIfCancellationRequested(); }
            if (options.GameFiles) { r.Report("Game files...", ct); await Task.Run(() => CopyGameFiles(rw, tmp, ct), ct); r.StepDone(); ct.ThrowIfCancellationRequested(); }
            var invalid = Path.GetInvalidFileNameChars(); var safeName = backupName; foreach (var c in invalid) safeName = safeName.Replace(c, '_');
            var zipName = string.IsNullOrWhiteSpace(safeName) || safeName.All(c => c == '_') ? DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") : safeName;
            Directory.CreateDirectory(_backupDir); var zipPath = Path.Combine(_backupDir, $"{zipName}.zip");
            await Task.Run(() => CreateZipWithProgress(tmp, zipPath, progress, ct), ct);
            progress.Report(new ProgressReport($"Done: {Helpers.FormatSize(new FileInfo(zipPath).Length)}", 100));
        }
        finally { if (Directory.Exists(tmp)) { try { Directory.Delete(tmp, true); } catch { } } }
    }

    public async Task RestoreAsync(string zipPath, string steamPath, IProgress<ProgressReport> progress, CancellationToken ct)
    {
        if (IsGameRunning())
            throw new InvalidOperationException("RimWorld is running. Please close the game before restoring.");

        var rw = Path.Combine(steamPath, @"steamapps\common\RimWorld");
        var ad = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", @"Ludeon Studios\RimWorld by Ludeon Studios");
        var tmp = Path.Combine(Path.GetTempPath(), $"RWRestore_{Path.GetRandomFileName()}");
        try
        {
            progress.Report(new ProgressReport("Extracting...", 5));
            await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tmp, System.Text.Encoding.UTF8, true), ct); ct.ThrowIfCancellationRequested();
            progress.Report(new ProgressReport("Config...", 25));
            await Task.Run(() => { var s = Path.Combine(tmp, "Config"); if (Directory.Exists(s)) { var d = Path.Combine(ad, "Config"); Directory.CreateDirectory(d); CopyDirectoryRecursive(s, d, ct); } }, ct); ct.ThrowIfCancellationRequested();
            progress.Report(new ProgressReport("Saves...", 50));
            await Task.Run(() => { var s = Path.Combine(tmp, "Saves"); if (!Directory.Exists(s)) return; var d = Path.Combine(ad, "Saves"); Directory.CreateDirectory(d); foreach (var f in Directory.EnumerateFiles(s, "*.rws")) File.Copy(f, Path.Combine(d, Path.GetFileName(f)), true); }, ct); ct.ThrowIfCancellationRequested();
            progress.Report(new ProgressReport("Local mods...", 60));
            await Task.Run(() => { var s = Path.Combine(tmp, "LocalMods"); if (!Directory.Exists(s)) return; foreach (var dir in Directory.EnumerateDirectories(s)) { ct.ThrowIfCancellationRequested(); var d = Path.Combine(rw, "Mods", Path.GetFileName(dir)); SafeDeleteDirectory(d); CopyDirectoryRecursive(dir, d, ct); } }, ct); ct.ThrowIfCancellationRequested();
            progress.Report(new ProgressReport("Workshop mods...", 75));
            await Task.Run(() => { var s = Path.Combine(tmp, "Workshop"); if (!Directory.Exists(s)) return; var ws = Path.Combine(steamPath, @"steamapps\workshop\content\294100"); if (!Directory.Exists(ws)) Directory.CreateDirectory(ws); foreach (var dir in Directory.EnumerateDirectories(s)) { ct.ThrowIfCancellationRequested(); var d = Path.Combine(ws, Path.GetFileName(dir)); Directory.CreateDirectory(d); CopyDirectoryRecursive(dir, d, ct); } }, ct); ct.ThrowIfCancellationRequested();
            progress.Report(new ProgressReport("Game info...", 85));
            await Task.Run(() => { var s = Path.Combine(tmp, "GameInfo"); if (!Directory.Exists(s)) return; foreach (var f in Directory.EnumerateFiles(s)) { var dest = Path.Combine(rw, Path.GetFileName(f)); SafeDelete(dest); File.Copy(f, dest, true); } }, ct); ct.ThrowIfCancellationRequested();
            progress.Report(new ProgressReport("Game files...", 92));
            await Task.Run(() => { var s = Path.Combine(tmp, "GameFiles"); if (!Directory.Exists(s)) return; foreach (var dir in Directory.EnumerateDirectories(s)) { ct.ThrowIfCancellationRequested(); CopyDirectoryRecursive(dir, Path.Combine(rw, Path.GetFileName(dir)), ct); } foreach (var file in Directory.EnumerateFiles(s)) { ct.ThrowIfCancellationRequested(); var dest = Path.Combine(rw, Path.GetFileName(file)); SafeDelete(dest); File.Copy(file, dest, true); } }, ct); ct.ThrowIfCancellationRequested();
            progress.Report(new ProgressReport("Player.log...", 95));
            await Task.Run(() => { var logSrc = Path.Combine(tmp, "Logs", "Player.log"); if (File.Exists(logSrc)) { var dest = Path.Combine(ad, "Player.log"); SafeDelete(dest); File.Copy(logSrc, dest, true); } }, ct);
            progress.Report(new ProgressReport("Done! Restart game to apply.", 100));
        }
        finally { if (Directory.Exists(tmp)) { try { Directory.Delete(tmp, true); } catch { } } }
    }

    // ===== Backup helpers =====
    private static void CopyConfig(string ad, string tmp, CancellationToken ct) { var src = Path.Combine(ad, "Config"); if (Directory.Exists(src)) CopyDirectoryRecursive(src, Path.Combine(tmp, "Config"), ct); }
    private static void CopySaves(string ad, string tmp, CancellationToken ct) { var src = Path.Combine(ad, "Saves"); if (!Directory.Exists(src)) return; var d = Path.Combine(tmp, "Saves"); Directory.CreateDirectory(d); foreach (var f in Directory.EnumerateFiles(src, "*.rws")) File.Copy(f, Path.Combine(d, Path.GetFileName(f)), true); }
    private static void CopyWorkshopMods(string ws, string tmp, StepReporter r, CancellationToken ct) { if (!Directory.Exists(ws)) return; var d = Path.Combine(tmp, "Workshop"); Directory.CreateDirectory(d); var dirs = Directory.GetDirectories(ws); for (int i = 0; i < dirs.Length; i++) { ct.ThrowIfCancellationRequested(); CopyDirectoryRecursive(dirs[i], Path.Combine(d, Path.GetFileName(dirs[i])), ct); if (i == dirs.Length - 1 || i % 30 == 0) r.Report($"Workshop: {i + 1}/{dirs.Length}", ct); } }
    private static void CopyLocalMods(string rw, string tmp, StepReporter r, CancellationToken ct) { var src = Path.Combine(rw, "Mods"); if (!Directory.Exists(src)) return; var d = Path.Combine(tmp, "LocalMods"); Directory.CreateDirectory(d); var dirs = Directory.GetDirectories(src).Where(x => !Path.GetFileName(x).Contains("Place mods here")).ToArray(); for (int i = 0; i < dirs.Length; i++) { ct.ThrowIfCancellationRequested(); CopyDirectoryRecursive(dirs[i], Path.Combine(d, Path.GetFileName(dirs[i])), ct); if (i == dirs.Length - 1 || i % 10 == 0) r.Report($"Local: {i + 1}/{dirs.Length}", ct); } }
    private static void CopyGameInfo(string rw, string tmp) { var d = Path.Combine(tmp, "GameInfo"); Directory.CreateDirectory(d); foreach (var f in new[] { "Version.txt", "LoadFolders.xml" }) { var fp = Path.Combine(rw, f); if (File.Exists(fp)) File.Copy(fp, Path.Combine(d, f), true); } var asm = Path.Combine(rw, @"RimWorldWin64_Data\Managed\Assembly-CSharp.dll"); if (File.Exists(asm)) File.WriteAllText(Path.Combine(d, "AssemblyVersion.txt"), FileVersionInfo.GetVersionInfo(asm).FileVersion ?? ""); }
    private static void CopyPlayerLog(string ad, string tmp) { var log = Path.Combine(ad, "Player.log"); if (!File.Exists(log)) return; var d = Path.Combine(tmp, "Logs"); Directory.CreateDirectory(d); File.Copy(log, Path.Combine(d, "Player.log"), true); }
    private static void CopyGameFiles(string rw, string tmp, CancellationToken ct)
    {
        var d = Path.Combine(tmp, "GameFiles");
        Directory.CreateDirectory(d);
        foreach (var file in Directory.EnumerateFiles(rw, "*", SearchOption.TopDirectoryOnly))
        {
            ct.ThrowIfCancellationRequested();
            File.Copy(file, Path.Combine(d, Path.GetFileName(file)), true);
        }
        foreach (var dir in Directory.EnumerateDirectories(rw, "*", SearchOption.TopDirectoryOnly))
        {
            ct.ThrowIfCancellationRequested();
            if (string.Equals(Path.GetFileName(dir), "Mods", StringComparison.OrdinalIgnoreCase))
                continue;
            CopyDirectoryRecursive(dir, Path.Combine(d, Path.GetFileName(dir)), ct);
        }
    }

    private static void CopyDirectoryRecursive(string source, string dest, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); SafeDelete(dest); Directory.CreateDirectory(dest);
        foreach (var dir in Directory.EnumerateDirectories(source))
        {
            ct.ThrowIfCancellationRequested();
            if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) continue;
            CopyDirectoryRecursive(dir, Path.Combine(dest, Path.GetFileName(dir)), ct);
        }
        var files = Directory.EnumerateFiles(source).ToArray();
        Parallel.ForEach(files, new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 4 }, file =>
        {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(file); var destPath = Path.Combine(dest, name);
            SafeDelete(destPath); File.Copy(file, destPath, true);
        });
    }

    private static void SafeDelete(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { if (File.Exists(path)) File.Delete(path); else if (Directory.Exists(path)) Directory.Delete(path, true); }
        catch (IOException) { var ts = DateTime.Now.ToString("yyyyMMddHHmmss"); if (File.Exists(path)) { try { File.Move(path, path + ".old_" + ts); } catch { } } else if (Directory.Exists(path)) { try { Directory.Move(path, path + "_old_" + ts); } catch { } } }
    }

    private static void SafeDeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        try { Directory.Delete(path, true); } catch (IOException) { var ts = DateTime.Now.ToString("yyyyMMddHHmmss"); try { Directory.Move(path, path + "_old_" + ts); } catch { } }
    }

    private static void CreateZipWithProgress(string srcDir, string destZip, IProgress<ProgressReport> progress, CancellationToken ct)
    {
        var files = Directory.GetFiles(srcDir, "*.*", SearchOption.AllDirectories); Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        int total = files.Length;
        var noCompressExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".ogg", ".wav", ".mp3", ".dds", ".zip", ".rar" };
        using (var zip = new ZipFile())
        {
            zip.AlternateEncoding = System.Text.Encoding.UTF8;
            zip.AlternateEncodingUsage = ZipOption.Always;
            zip.UseZip64WhenSaving = Zip64Option.Always; zip.ParallelDeflateThreshold = 0; zip.CompressionLevel = IonicCompressionLevel.BestSpeed; zip.BufferSize = 131072;
            for (int i = 0; i < files.Length; i++) { ct.ThrowIfCancellationRequested(); var file = files[i]; var relative = file.Substring(srcDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/'); var entry = zip.AddFile(file, ""); entry.FileName = relative; entry.CompressionLevel = noCompressExts.Contains(Path.GetExtension(file)) ? IonicCompressionLevel.None : IonicCompressionLevel.BestSpeed; }
            zip.SaveProgress += (_, e) => { ct.ThrowIfCancellationRequested(); if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry) { int current = e.EntriesSaved + 1; if (total > 0) progress.Report(new ProgressReport($"Compressing: {current}/{total}", 95 + (int)((double)current / total * 5))); } else if (e.EventType == ZipProgressEventType.Saving_Completed) progress.Report(new ProgressReport("Compressing: done", 100)); };
            zip.Save(destZip);
        }
    }

    private class StepReporter { private readonly IProgress<ProgressReport> _p; private readonly int _t; private int _c; public StepReporter(IProgress<ProgressReport> p, int t) { _p = p; _t = t; } public void Report(string m, CancellationToken ct) { _p.Report(new ProgressReport(m, _c * 95 / _t)); } public void StepDone() { _c++; } }
}
