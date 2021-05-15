using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CoreNodeWidgetCompiler
{
    class WidgetCompiler
    {
        public static string ModelRefrences(string PathToModels = "/CoreNode")
        {
            return "import LayoutModel from \"" + PathToModels + "/Models/LayoutModel.js\";\n" +
            "import WidgetModel from \"" + PathToModels + "/Models/WidgetModel.js\";\n" +
            "import Core from \"" + PathToModels + "/Models/Core.js\";\n\n";
        }
        public static string Template(string Type, string Name, bool IsDefault)
        {
            if (Type == "widget")
            {
                return "export" + (IsDefault ? " default" : "") + " class " + Name + " extends WidgetModel {\n";
            }
            else if (Type == "layout")
            {
                return "export" + (IsDefault ? " default" : "") + " class " + Name + " extends LayoutModel {\n";
            }
            else if (Type == "")
            {
                if (Name.EndsWith("()"))
                {
                    Name = Name.Substring(0, Name.Length - 2) + " = ";
                }
                else
                {
                    Name += " = ";
                }
                return "export" + (IsDefault ? " default" : "") + " const " + Name;

            }

            Console.WriteLine(Type + "is not recognizedfor type");
            Console.WriteLine("Known types are 'Widget' and 'layout' and empty");
            return "";
        }
        public static string Padding(string Content, int Count)
        {
            if (string.IsNullOrEmpty(Content))
                return "";

            return new string('\t', Count) + Content.Replace("\n", "\n" + new string('\t', Count));
        }
        public static string ContainCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return "";
            }

            return "\n" + Padding(code, 1) + "\n";
        }
        public static Dictionary<string, Argument> NodeArguments(HtmlNode Node, bool IsInContext = true)
        {
            Dictionary<string, Argument> Arguments = new Dictionary<string, Argument>();

            foreach (var attr in Node.Attributes)
            {
                var _tmp = attr.OriginalName.Split(':');
                int _type = 0;

                string _name = "";
                string _init = "";

                if (_tmp.Count() > 1)
                {
                    _name = _tmp[1];

                    if (!string.IsNullOrEmpty(attr.Value))
                    {
                        _init = " = " + attr.Value;
                    }

                    switch (_tmp[0])
                    {
                        case "var":
                            break;
                        case "widget":
                            _type = 1;
                            break;
                        case "widget[]":
                            _type = 2;
                            break;
                    }
                }
                else
                {
                    _name = _tmp[0];

                    if (!string.IsNullOrEmpty(attr.Value))
                    {
                        _init = " = " + attr.Value;
                    }
                }

                Arguments.Add(_name, new Argument()
                {
                    Type = _type,
                    Initial = _init,
                    InContext = IsInContext,
                });
            }

            return Arguments;
        }
        public static string Attribute(HtmlNode Node)
        {
            if (Node == null) return "{}";
            return "{" +
            string.Join(", ", Node.Attributes.ToList().ConvertAll(Attr => "\"" + Attr.OriginalName + "\":\"" + Attr.Value + "\"").Where(st => st != "").ToList()) +
            "}";
        }
        public static string Minify(string HTMLContent)
        {
            return Regex.Replace(HTMLContent, @"[\s]*\n[\s]*", "");
        }
        public static string PreCompile(string HTMLContent, WidgetArgument Setting)
        {
            var ArgumentMatches = Regex.Matches(HTMLContent, @"\((<(?:[\S\s](?!\(<.+>\)))*?>)\)", RegexOptions.Singleline);

            while (ArgumentMatches.Count > 0)
            {
                foreach (Match match in ArgumentMatches)
                {
                    HtmlDocument htmlArg = new HtmlDocument();
                    htmlArg.LoadHtml(match.Groups[1].Value);

                    try
                    {
                        htmlArg.DocumentNode.SelectNodes("//text()[not(normalize-space())]").ToList().ForEach(element => element.Remove());
                    }
                    catch { }

                    HTMLContent = HTMLContent.Replace(match.Groups[1].Value, CompileNode(htmlArg.DocumentNode.FirstChild, 1, false));
                    //HTMLContent = HTMLContent.Replace(match.Groups[0].Value, CompileNode(htmlArg.DocumentNode.FirstChild, 1, false));
                }

                ArgumentMatches = Regex.Matches(HTMLContent, @"\((<(?:[\S\s](?!\(<.+>\)))*?>)\)", RegexOptions.Singleline);
            }

            return HTMLContent;
        }
        public static string CompileNode(HtmlNode Node, int Depth = 0, bool AddEnding = true)
        {
            if (Depth < 0 || Node == null)
                return "";

            string attributes = Attribute(Node);

            string Childs = "";
            var tmp = "";
            string Ender = AddEnding ? (Depth > 0 ? "," : ";") : "";

            if (Node.OriginalName == "#text")
            {
                return "\"" + Regex.Replace(Node.InnerText, @"\r\n[\s]*", "") + "\"" + Ender;
            }

            if (Node.ChildNodes.Count() > 0)
            {
                tmp = string.Join("\n", Node.ChildNodes.ToList().ConvertAll(node => CompileNode(node, Depth + 1)));
            }

            Childs += (!string.IsNullOrEmpty(tmp) && !string.IsNullOrEmpty(Childs) ? "\n" : "") + tmp;

            Childs = ContainCode(Childs);

            string element = "Core.Node(\"" + Node.OriginalName + "\", " + attributes + ", [" +
                Childs +
                "], el => {})" + Ender;

            return element;
        }
        public static string CompileFile(string FileAddress, string Destinationfolder)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            WidgetArgument Setting = new WidgetArgument();
            string JsFilePath = "";
            string HtmlContent = "";
            try{
                HtmlContent = File.ReadAllText(FileAddress);
            }
            catch(Exception ex){
                _= ex;
            }
            string Output = ModelRefrences();

            if (FileAddress.EndsWith(".html"))
            {
                JsFilePath = Path.Combine(Destinationfolder, Path.GetFileName(FileAddress)).Replace(".html", ".js");
            }
            else if (FileAddress.EndsWith(".js"))
            {
                JsFilePath = Path.Combine(Destinationfolder, Path.GetFileName(FileAddress));
                HtmlContent = Output + Setting.ImplementArguments(PreCompile(HtmlContent, Setting));

                if (!string.IsNullOrEmpty(Destinationfolder))
                {
                    File.WriteAllText(JsFilePath, HtmlContent);
                    return "";
                }
                else
                {
                    return HtmlContent;
                }
            }
            else
            {
                Console.WriteLine("Extension not recognized.");
                Console.WriteLine("Extensions can either be .html or .js");
                return "";
            }

            /*  */
            //HtmlContent = Minify(HtmlContent);
            //File.WriteAllText(JsFilePath, HtmlContent);

            /**/
            HtmlContent = PreCompile(HtmlContent, Setting);

            htmlDoc.OptionOutputOriginalCase = true;

            htmlDoc.LoadHtml(HtmlContent);

            if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() > 0)
            {
                Console.WriteLine("Errors were found");
                foreach (var err in htmlDoc.ParseErrors)
                {
                    Console.WriteLine("Line {0}, Col {1} : " + err.Reason, err.Line, err.LinePosition);
                }
                return "";
            }

            if (htmlDoc.DocumentNode == null)
            {
                Console.WriteLine("No document node.");
                return "";
            }

            try
            {
                htmlDoc.DocumentNode.SelectNodes("//text()[not(normalize-space())]").ToList().ForEach(element => element.Remove());
            }
            catch { }

            foreach (var node in htmlDoc.DocumentNode.ChildNodes)
            {

                string Functions = "";

                bool IsDefault = false;

                var tmp = node.OriginalName.Split(':');

                if (tmp.Count() == 1)
                {
                    Setting.Type = "";
                    Setting.Name = tmp[0];
                }
                else if (tmp.Count() == 2)
                {
                    if (tmp[0] == "default")
                    {
                        Setting.Type = "";
                    }
                    else
                    {
                        Setting.Name = tmp[1];
                    }

                    Setting.Name = tmp[1];
                }
                else if (tmp.Count() == 3)
                {
                    Setting.Type = tmp[1];
                    Setting.Name = tmp[2];
                }

                if (tmp != null && tmp.Count() > 0 && tmp[0] == "default") IsDefault = true;

                Setting.Arguments = NodeArguments(node, string.IsNullOrEmpty(Setting.Type) ? false : true);

                var Constructor = node.ChildNodes.Where(el => el.OriginalName == "constructor").FirstOrDefault();

                if (Constructor != null)
                {
                    Setting.ConstructorContent = Constructor.InnerText;
                    Constructor.Remove();
                }

                if (Setting.Type != "")
                {
                    foreach (var child in node.ChildNodes/**/)
                    {

                        string definer = child.OriginalName.Replace(':', ' ');

                        WidgetArgument tmparg = new WidgetArgument()
                        {
                            Arguments = NodeArguments(child, false),
                        };

                        tmparg.Arguments.ToList().ForEach(kv => Setting.Arguments.Add(kv.Key, kv.Value));

                        if (definer.EndsWith("()"))
                        {
                            definer = definer.Replace("()", " = (" + tmparg.ConstructorArgs + ") => ");
                        }
                        else
                        {
                            definer += " = ";
                        }

                        Functions += ContainCode(definer + Setting.ImplementArguments(CompileNode(child.FirstChild)));

                        tmparg.Arguments.Keys.ToList().ForEach(k => Setting.Arguments.Remove(k));
                    }
                }
                else
                {
                    if (Setting.Name == "#text")
                    {
                        Functions += Setting.ImplementArguments(CompileNode(node));
                    }
                    else
                    {
                        Functions += Setting.ImplementArguments(CompileNode(node.FirstChild));
                    }
                }

                if (Setting.Type == "layout")
                {
                    Output += "export" + (IsDefault ? " default" : "") + " class " + Setting.Name +
                    " extends LayoutModel {" + ContainCode(Setting.Constructor) +
                    Functions +
                    "}\n";

                }
                else if (Setting.Type == "widget")
                {
                    Output += "export" + (IsDefault ? " default" : "") + " class " + Setting.Name +
                    " extends WidgetModel {" + ContainCode(Setting.Constructor) +
                    Functions +
                    "}\n";

                }
                else if (Setting.Type == "")
                {

                    if (Setting.Name == "#text")
                    {
                        Output += Functions;
                    }

                    string definer = "export" + (IsDefault ? " default " : " const ") + Setting.Name;

                    if (definer.EndsWith("()"))
                    {
                        definer = definer.Replace("()", " = (" + Setting.ConstructorArgs + ") => ");
                    }
                    else
                    {
                        definer += " = ";
                    }

                    Output += definer + Functions + "\n";
                }
            }

            File.WriteAllText(JsFilePath, Output);
            return "Successful";
        }
    }
}
