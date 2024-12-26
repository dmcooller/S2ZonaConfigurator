using S2ZonaConfigurator.Enums;
using S2ZonaConfigurator.Models;
using System.Text.Json;
using System;

namespace S2ZonaConfigurator.Helpers;

public static class Printer
{
    private const string BORDER_TOP = "╔";
    private const string BORDER_MIDDLE = "╠";
    private const string BORDER_BOTTOM = "╚";
    private const string BORDER_VERTICAL = "║";
    private const string BORDER_HORIZONTAL = "═";
    private const int BORDER_WIDTH = 105;

    private static void PrintBorder(string borderChar) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{borderChar}{new string(BORDER_HORIZONTAL[0], BORDER_WIDTH)}");
    }

    private static void PrintSegmentBorder()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{BORDER_VERTICAL} ");
    }

    private static void PrintLine(string text, ConsoleColor? color = null)
    {
        PrintSegmentBorder();

        Console.ForegroundColor = color ?? ConsoleColor.White;

        Console.WriteLine($"{text}");

        if (color.HasValue)
            Console.ResetColor();
    }

    private static void PrintSection(string title, Action contentPrinter)
    {
        Console.WriteLine();

        PrintBorder(BORDER_TOP);
        PrintLine(title);
        PrintBorder(BORDER_MIDDLE);

        Console.ResetColor();

        contentPrinter();

        PrintBorder(BORDER_BOTTOM);
        Console.ResetColor();
    }

    public static void PrintColoredField(string label, string value, ConsoleColor valueColor)
    {
        PrintSegmentBorder();

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{label}: ");
        Console.ForegroundColor = valueColor;
        Console.WriteLine(value);
        Console.ResetColor();
    }

    public static void PrintModHeader(string modFile, string version, int totalModsProcessed) =>
        PrintSection($"Mod [{totalModsProcessed}]: {Path.GetFileName(modFile)}", () =>
            PrintLine($"Version: {version}", ConsoleColor.White));

    public static void PrintActionProgress(int currentAction, int totalActions, ModActionData actionData)
    {
        PrintSegmentBorder();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"Action [{currentAction}/{totalActions}] ");

        PrintColoredSegments(
        [
            ("Type: ", ConsoleColor.White),
            ($"{actionData.Type}", ConsoleColor.Green)
        ]);

        if (!string.IsNullOrEmpty(actionData.Path))
        {
            PrintColoredSegments(
            [
                ("Path: ", ConsoleColor.White),
                (actionData.Path, ConsoleColor.DarkCyan)
            ]);
        }

        if (actionData.Type == ActionType.Replace && actionData.Value != null)
        {
            PrintColoredSegments(
            [
                ("Value: ", ConsoleColor.White),
                (actionData.Value.ToString() ?? "null", ConsoleColor.DarkCyan)
            ]);
        }

        Console.WriteLine();
    }

    public static void PrintColoredSegments(IEnumerable<(string text, ConsoleColor color)> segments)
    {
        PrintSegmentBorder();

        foreach (var (text, color) in segments)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
        }
        Console.ResetColor();
    }

    public static void PrintModFooter(bool success = true)
    {
        var status = success ? "Successfully processed" : "Failed to process";
        var color = success ? ConsoleColor.Green : ConsoleColor.Red;

        PrintLine("", color);
        PrintLine($"Status: {status}", color);
        PrintBorder(BORDER_BOTTOM);
        Console.ResetColor();
    }

    public static void PrintFinalSummary(int totalModsProcessed, int successCount, int failureCount) =>
        PrintSection("Processing Summary", () =>
        {
            PrintColoredField("Total Mods Processed", totalModsProcessed.ToString(), ConsoleColor.White);
            PrintColoredField("Successful", successCount.ToString(), ConsoleColor.Green);
            PrintColoredField("Failed", failureCount.ToString(), ConsoleColor.Red);
        });

    public static void PrintPakStatus(string pakPath, bool success) =>
        PrintSection("Pak Creation Status", () =>
        {
            PrintColoredField("Pak Path", pakPath, ConsoleColor.Yellow);
            PrintColoredField("Status",
                success ? "Successfully created" : "Failed to create",
                success ? ConsoleColor.Green : ConsoleColor.Red);
        });

    public static void PrintExceptionSection(Exception ex) =>
        PrintSection("Error", () => PrintLine(ex.Message, ConsoleColor.Red));
    public static void PrintErrorSection(string message) =>
        PrintSection("Error", () => PrintLine(message, ConsoleColor.Red));

    public static void PrintInfoSection(string message) =>
        PrintSection("Info", () => PrintLine(message, ConsoleColor.Green));

    public static void PrintErrorMessage(string message) =>
        PrintLine(message, ConsoleColor.Red);

    public static void PrintConflicts(List<ModConflict> conflicts)
    {
        PrintSection("Mod Conflicts Detected", () =>
        {
            foreach (var conflict in conflicts)
            {
                // Print file information
                PrintColoredSegments([
                    ("Config File: ", ConsoleColor.White),
                (conflict.ConfigFile, ConsoleColor.Yellow)
                ]);
                Console.WriteLine();

                // Print conflict type
                if (conflict.IsReplaceConflict)
                {
                    PrintColoredField("Conflict Type", "Replace conflict", ConsoleColor.Red);
                    if (conflict.Path == null)
                    {
                        PrintLine("WARNING: File has both Replace and other modification types", ConsoleColor.Yellow);
                    }
                }
                else
                {
                    PrintColoredField("Path", conflict.Path ?? "N/A", ConsoleColor.DarkCyan);
                }

                // Print Mod 1 details
                PrintColoredSegments([
                    ("Mod 1: ", ConsoleColor.White),
                (Path.GetFileName(conflict.ModFile1), ConsoleColor.Magenta)
                ]);
                Console.WriteLine();

                PrintColoredField("Action", conflict.Action1.ToString(), ConsoleColor.Green);
                PrintValueDetails(conflict.Value1);

                // Print Mod 2 details
                PrintColoredSegments([
                    ("Mod 2: ", ConsoleColor.White),
                (Path.GetFileName(conflict.ModFile2), ConsoleColor.Magenta)
                ]);
                Console.WriteLine();

                PrintColoredField("Action", conflict.Action2.ToString(), ConsoleColor.Green);
                PrintValueDetails(conflict.Value2);

                // Add separator between conflicts if not the last one
                if (conflict != conflicts.Last())
                {
                    PrintLine(new string('-', 50), ConsoleColor.DarkGray);
                }
            }
        });
    }

    private static void PrintValueDetails(object? value)
    {
        if (value == null) return;

        if (value is Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("old", out var oldValue))
            {
                PrintColoredField("Old Value", oldValue?.ToString() ?? "null", ConsoleColor.DarkYellow);
            }
            if (dict.TryGetValue("new", out var newValue))
            {
                PrintColoredField("New Value", newValue?.ToString() ?? "null", ConsoleColor.DarkYellow);
            }
        }
        else
        {
            PrintColoredField("Value", value.ToString() ?? "null", ConsoleColor.DarkYellow);
        }
    }
}