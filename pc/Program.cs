using System;
using System.CommandLine;
using System.IO;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

//options

var outputOption = new Option<FileInfo>("--output", "file path and name");
var languageOption = new Option<string>("--language", "languages to be bundle");
var noteOption = new Option<bool>("--note", "add name and path of the source code");
var sortOption = new Option<string>("--sort", "Sort the code alphabetically or by type");
var RemoveEmptyLinesOption = new Option<bool>("--remove-empty-lines", "remove empty lines");
var authorOption = new Option<string>("--author", "add the author to the file");
//aliases
outputOption.AddAlias("-o");
languageOption.AddAlias("-l");
noteOption.AddAlias("-n");
sortOption.AddAlias("-s");
RemoveEmptyLinesOption.AddAlias("-re");
authorOption.AddAlias("-a");
//required
outputOption.IsRequired = true;
languageOption.IsRequired = true;
//default value
noteOption.SetDefaultValue(false);
sortOption.SetDefaultValue("abc");
RemoveEmptyLinesOption.SetDefaultValue(false);
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
//add the options to the bundle
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(RemoveEmptyLinesOption);
bundleCommand.AddOption(authorOption);
//
string currentPath = Directory.GetCurrentDirectory();
List<string> allFolders = Directory.GetFiles(currentPath,"", SearchOption.AllDirectories).Where(file => !file.Contains("bin") && !file.Contains("Debug")).ToList();

string[] allLanguages = { "c", "c++", "java", "c#", "javascript", "html", "css","pyton" };
string[] extensions = { ".c", ".cpp", ".java", ".cs", ".js", ".html", ".css",".py" };

bundleCommand.SetHandler((output, language, note, sort, toRemove, author) =>
{
    string[] languages = GetLanguages(language, extensions, allLanguages);
    List<string> files = allFolders.Where(file => languages.Contains(Path.GetExtension(file))).ToList();
    //validation sort
    if (!sort.Equals("abc") && !sort.Equals("type"))
    {
        Console.WriteLine("the value is invalid (" + sort + "), the sort is alphabetically by default");
        sort = "abc";
    }
    SortFiles(sort, files);
    WriteToFile(output.FullName, author, note, files, toRemove, currentPath);
    Console.WriteLine("note: " + note + " sort: " + sort + " remove: " + toRemove + " author: " + author);
}, outputOption, languageOption, noteOption, sortOption, RemoveEmptyLinesOption, authorOption);

var create_rspCommand = new Command("create-rsp", "create respond file for bundle command");
create_rspCommand.SetHandler(() => {
    string output, languages, author, sort,note, toRemove;
    StreamWriter file = new StreamWriter("responseFile.rsp");
    Console.WriteLine("enter the name of the file and the path(not required- by default in this path) ");
    output=Console.ReadLine();
    while (output.Length==0)
    {
        Console.WriteLine("this field is required!!!");
        output = Console.ReadLine();
    }
    Console.WriteLine("enter the languages you want to include or all to include all languages");
    languages=Console.ReadLine();
    while(languages.Length==0)
    {
        Console.WriteLine("this field is reqired!!!");
        languages =Console.ReadLine();
    }
    Console.WriteLine("enter how to sort (abc / type) ");
    sort=Console.ReadLine();    
    Console.WriteLine("do you want to write the source file? (y/n)");
    note=Console.ReadLine();
    Console.WriteLine("do you wand to enter the author name? enter it! ");
    author=Console.ReadLine();
    Console.WriteLine("do you want to remove empty lines? (y/n)");
    toRemove=Console.ReadLine();

    file.Write("bundle");
    file.Write(" -o "+output);
    file.Write(" -l \""+languages+"\" ");
    if (author.Length>0) 
        file.Write(" -a "+author);
    if(sort.Length>0)
        file.Write(" -s "+sort);
    if(note.Equals("y")||note.Equals("Y")||note.Equals("yes"))
        file.Write(" -n "+"true");
    if(toRemove.Equals("y")||toRemove.Equals("Y")||toRemove.Equals("yes"))
        file.Write(" -re "+"true");
    file.Close();
    Console.Write("you finish!!! new enter pc @responseFile");
});
var rootCommand = new RootCommand("Root command for file bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(create_rspCommand);
rootCommand.InvokeAsync(args);

static string[] GetLanguages(string language, string[] extensions, string[] allLanguages)
{
    if (language.Equals("all"))
        return extensions;
    string[] languages = language.Split(' ');
    for (int i = 0; i < languages.Length; i++)
    {
        for (int j = 0; j < allLanguages.Length; j++)
        {
            if (languages[i].Equals(allLanguages[j]))
            {
                languages[i] = extensions[j];
                break;
            }
        }
    }
    return languages;
}
static void SortFiles(string sortType, List<string> files)
{
    for (int i = 0; i < files.Count; i++)
    {
        for (int j = 0; j < files.Count - i - 1; j++)
        {
            if ((sortType.Equals("abc") && Path.GetFileName(files[j]).CompareTo(Path.GetFileName(files[j + 1])) > 0) ||
                ((sortType.Equals("type") && Path.GetExtension(files[j]).CompareTo(Path.GetExtension(files[j + 1])) > 0)))
            {

                string temp = files[j];
                files[j] = files[j + 1];
                files[j + 1] = temp;
            }
        }
    }
}
static void WriteToFile(string output,string author,bool note,List<string>files,bool toRemove,string currentPath)
{
    try
    {
        StreamWriter sw = new StreamWriter(output);
        if (author != null)
            sw.WriteLine("// author: " + author);
        if (note == true)
            sw.WriteLine("// source file: " + currentPath + "  name: " + Path.GetFileName(currentPath));
        for (int i = 0; i < files.Count; i++)
        {
            sw.WriteLine("==" + Path.GetFileName(files[i]));
            StreamReader reader = new StreamReader(files[i]);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if ((toRemove && line.Length > 0) || !toRemove)
                    sw.WriteLine(line);
            }
            reader.Close();
        }
        sw.Close();
        Console.WriteLine("The file was created successfully");
    }
    catch (System.IO.DirectoryNotFoundException) { Console.WriteLine("error: file path is invalid"); }
}