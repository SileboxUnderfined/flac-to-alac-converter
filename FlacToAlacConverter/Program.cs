using System.Diagnostics;
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;

namespace FlacToAlacConverter;
public class Program {
    public static async Task Main(string[] args) {
        string? platform;
        string target_dir;
        string? ffmpeg_path;

        platform = GetPlatform();
        if (platform is null) {
            Console.WriteLine("your OS is not supported!");
            return;
        }

        if (args.Length == 0) target_dir = AppContext.BaseDirectory;
        else target_dir = args[0];

        if (!Directory.Exists(target_dir)) {
            Console.WriteLine("Target Directory does not exists.");
            return;
        }

        if (!IsFFmpegInstalled()) {
            Console.WriteLine("Download FFmpeg or make sure it's in the PATH!");
            return;
        }

        ffmpeg_path = GetFFmpegPath();

        if (ffmpeg_path is null) {
            Console.WriteLine("Could not find FFmpeg in PATH or binary does not have rights!");
            return;
        }

        string[] subdirs = Directory.GetDirectories(target_dir);

        if (subdirs.Count() > 0) {
            foreach (string dir in subdirs) {
                if (!dir.Contains("alac")) 
                    await ProcessFilesInDirectory(dir);
            }
        } else {
            await ProcessFilesInDirectory(target_dir);
        }

        Console.WriteLine("All files were processed!");
    }

    private async static Task ProcessFilesInDirectory(string target_dir) {
        string[] files = Directory.GetFiles(target_dir);

        if (Directory.GetDirectories(target_dir).Any(d => d.Contains("alac"))) return;
        
        DirectoryInfo output_dir = Directory.CreateDirectory(target_dir + "/alac");

        foreach (string file in files) {
            if (!file.Contains(".flac")) continue;
            string filename = file.Split("/").Last().Split(".").First();
            string output_path = output_dir.FullName + "/" + filename + ".m4a";

            await FFMpegArguments
                .FromFileInput(file)
                .OutputToFile(output_path, true, options => options
                    .WithArgument( new AudioCodecArgument("alac"))
                    .DisableChannel(Channel.Video)
                    .WithArgument(new AudioBitrateArgument(32))
                    .WithFastStart())
                    
                .ProcessAsynchronously();

            Console.WriteLine($"Processed {filename}.flac successfully!");
        }
    }

    private static bool IsFFmpegInstalled() {
        try {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process {StartInfo = startInfo}) {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrWhiteSpace(output) || !string.IsNullOrWhiteSpace(error);
            }
        } catch {
            return false;
        }
    }

    private static string? GetPlatform() {
        if (OperatingSystem.IsWindows()) return "windows";
        else if (OperatingSystem.IsMacOS()) return "macos";
        else return null;
    }

    private static string? GetFFmpegPath() {
        string command = OperatingSystem.IsWindows() ? "where" : "which";
        ProcessStartInfo psi = new ProcessStartInfo {
            FileName = command,
            Arguments = "ffmpeg",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(psi)) {
            process.WaitForExit();
            string output = process.StandardOutput.ReadLine();

            return !string.IsNullOrEmpty(output) ? output : null;
        }
    }
}