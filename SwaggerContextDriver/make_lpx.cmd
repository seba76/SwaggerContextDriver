@echo off
cd bin/Debug/
zip ../../SwaggerContextDriver.lpx *.* it/* -x LINQPad.exe -x LINQPad.exe.config -x Newtonsoft.Json.xml -x NJsonSchema*.xml -x NSwag*.xml -x DotLiquid.xml
cd ../../