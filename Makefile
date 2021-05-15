Package:
	dotnet add package HtmlAgilityPack
	dotnet add package Newtonsoft.Json

publish:
	dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true