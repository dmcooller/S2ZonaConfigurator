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

        if (color.HasValue)
            Console.ForegroundColor = color.Value;
        else Console.ForegroundColor = ConsoleColor.White;

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

        if (actionData.Type == ActionType.Replace)
        {
            PrintColoredSegments(
                [
                    ("Value: ", ConsoleColor.White),
                    (actionData.Value.ToString(), ConsoleColor.DarkCyan)
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

    public static void PrintExceptionMessage(Exception ex) =>
        PrintSection("Error", () => PrintLine(ex.Message, ConsoleColor.Red));

    public static void PrintInfoSection(string message) =>
        PrintSection("Info", () => PrintLine(message, ConsoleColor.Green));

    public static void PrintErrorMessage(string message) =>
        PrintLine(message, ConsoleColor.Red);

}