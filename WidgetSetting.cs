using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoreNodeWidgetCompiler
{
    class Argument
    {
        public int Type { get; set; }
        public string Initial { get; set; }
        public bool InContext { get; set; }
        public static string[] InitValue 
        { 
            get 
            { 
                return new string[] { "", "", " = []"};
            } 
        }
    }
    class WidgetArgument
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, Argument> Arguments { get; set; }
        public Dictionary<string, Argument> ScopedArguments { get; set; }
        public string ConstructorContent { get; set; }
        public string ImplementArgument(string name, Argument arg)
        {
            switch (arg.Type)
            {
                case 0:
                    return (arg.InContext ? "this.":"") + name;     
                case 1:
                    return "Core.BuildWidgets([" + (arg.InContext ? "this.":"") +  name + "])[0]";
                case 2:
                    return "...Core.BuildWidgets(" + (arg.InContext ? "this.":"") + name + ")";
                default:
                    return "";
            }
        }
        public string UnimplementArgument(string name, Argument arg)
        {
            string ret = "";
            switch (arg.Type)
            {
                case 0:
                    ret = "\"#" + name + "#\"";
                    break;
                case 1 or 2:
                    ret = "Core.Node(\"" + name + "\", {}, [], el => {})";
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }
        public string ImplementArguments(string BuildContext, bool ClassContent = true)
        {
            /*  */
            BuildContext = BuildContext.Replace("Core.Node(\"app\", {}, [], el => {})", "Core.App()");

            /*  */
            while(Regex.Match(BuildContext, @"[""]?[\s\t\n]*#{.*?}#[\t\s\n]*[""]?", RegexOptions.Singleline).Captures.Count() > 0)
            {
                BuildContext = Regex.Replace(BuildContext, @"[""]?[\s\t\n]*#{(.*?)}#[\t\s\n]*[""]?", "$+", RegexOptions.Singleline);
            }

            /*  */
            BuildContext = BuildContext.Replace("\\#", "#");

            if (Arguments == null)
                return BuildContext;
            
            Arguments.ToList().ForEach(arg => {
                BuildContext = BuildContext.Replace(UnimplementArgument(arg.Key, arg.Value), ImplementArgument(arg.Key, arg.Value));
            });

            return BuildContext;
        }
        public string ConstructorArgs
        {
            get
            {
                return string.Join(", ", Arguments.ToList().ConvertAll(arg => {
                    return arg.Key + Regex.Replace(arg.Value.Initial, @"[""]?#{(.*?)}#[""]?", "$+", RegexOptions.Singleline);;
                }));
            }
        }
        public string ConstructorBody
        {
            get
            {
                string userConstructor = Regex.Replace(ConstructorContent, @"\n[\s]*", "\n");

                if(userConstructor.EndsWith("\r\n")){
                    userConstructor = userConstructor.Substring(0, userConstructor.Length - 2);
                }

                return (string.Join("\n", Arguments.ToList().ConvertAll(arg => {
                    return "this." + arg.Key + " = " + arg.Key + ";";
                })) + userConstructor);
            }
        }
        public string Constructor
        {
            get
            {
                if (Arguments == null || Arguments.Count() <= 0)
                    return "constructor()\n{\n" +
                    "\tsuper();\n}";

                return "constructor(" + ConstructorArgs + ")\n{\n" +
                    "\tsuper();" + WidgetCompiler.ContainCode(ConstructorBody) + "}";
            }
        }
        public string FieldsBody
        {
            get
            {
                return string.Join("\n", Arguments.ToList().ConvertAll(arg => {
                    return arg.Key + " : \"" + arg.Value.Initial + "\",";
                }));
            }
        }
    }
}
