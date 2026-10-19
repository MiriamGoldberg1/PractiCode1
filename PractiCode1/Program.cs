using System.CommandLine;

var RootCommand = new RootCommand("Mmy root command for bandle");

var bundleCMD = new Command("bundle", "packing some code file for single file");

RootCommand.Subcommands.Add(bundleCMD);

var bundleOption = new Option<List<string>>("--languages", "-l") //-l => alias for --languages
{
    Description = "or list of languages, or 'all' ",
    Required = true,
    AllowMultipleArgumentsPerToken = true
};

bundleCMD.Options.Add(bundleOption);


//המרה משם השפה לסיומת קובץ
var languageExtensions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
{
    ["csharp"] = new[] { ".cs" },
    ["java"] = new[] { ".java" },
    ["python"] = new[] { ".py" },
    ["javascript"] = new[] { ".js" },
    ["html"] = new[] { ".html" },
    ["css"] = new[] { ".css" }
};



var pathOption = new Option<DirectoryInfo>("--path", "-p")                    // אופציה לניתוב תיקיית המקור
{
    Description = "Source folder path (default: current folder)",             // תיאור שיוצג ב-help
    DefaultValueFactory = _ => new DirectoryInfo(Directory.GetCurrentDirectory()) // ברירת מחדל: התיקייה שבה ה-CMD פתוח
};
bundleCMD.Options.Add(pathOption);                                            // הוספת האופציה לפקודה

// what will happen when someone will write in cmd "bundle":
//as sedaHaundle
bundleCMD.SetAction((parseResult) =>
{
    var languagesList = parseResult.GetValue(bundleOption); // takes the languages from user to list called languagesList

    bool includeAll = languagesList.Any(l => l.Equals("all", StringComparison.OrdinalIgnoreCase)); // האם הוקלד all

    var extensions = includeAll == true                               // אם נבחר all
        ? languageExtensions.Values.SelectMany(e => e).ToList() // לוקחים את כל הסיומות מכל השפות
        : languagesList                                        // אחרת
            .Where(l => languageExtensions.ContainsKey(l))     // משאירים רק שפות מוכרות
            .SelectMany(l => languageExtensions[l])            // ממירים כל שפה לסיומות שלה
            .ToList();                                         // והופכים לרשימה

    var sourceDir = parseResult.GetValue(pathOption);       // שליפת הניתוב שהמשתמש הקליד (או ברירת המחדל)

    if (!sourceDir.Exists)                                  // בדיקה שהתיקייה קיימת
    {
        Console.WriteLine($"Folder not found: {sourceDir.FullName}"); // הודעת שגיאה
        return;                                             // יציאה מהפונקציה
    }

    var currentDir = sourceDir.FullName;                    // הניתוב המלא של התיקייה
    var output = new FileInfo(Path.Combine(currentDir, "bundleOutput.txt"));   // הפלט נוצר בתיקיית המקור

    var files = Directory.GetFiles(currentDir, "*", SearchOption.AllDirectories) // כל הקבצים בתיקייה ובתתי-התיקיות
        .Where(f => extensions.Contains(Path.GetExtension(f)))                // רק קבצים עם סיומת מתאימה
        .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))        // מדלגים על תיקיות קומפילציה
        .ToList();                                                            // הופכים לרשימה

    if (files.Count == 0)                                      // אם לא נמצאו קבצים
    {
        Console.WriteLine("No matching files found.");         // הודעה למשתמש
        return;                                                // יציאה מהפונקציה
    }

    using var writer = new StreamWriter(output.FullName);      // פתיחת קובץ הפלט לכתיבה (נסגר אוטומטית בסוף)

    foreach (var file in files)                                // מעבר על כל קובץ שנמצא
    {
        writer.WriteLine($"// ===== {Path.GetFileName(file)} ====="); // כותרת עם שם הקובץ
        writer.WriteLine(File.ReadAllText(file));              // תוכן הקובץ המלא
        writer.WriteLine();                                    // שורה ריקה להפרדה
    }

    Console.WriteLine($"Bundle created: {output.FullName}");   // הודעת הצלחה עם מיקו

});

return RootCommand.Parse(args).Invoke();