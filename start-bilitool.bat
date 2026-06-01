@echo off
title BiliTool.Vn - Server
echo ================================================
echo  BiliTool.Vn dang khoi dong tren port 5050...
echo ================================================
cd /d "d:\Project Windsurf\Bilitool"
dotnet run --project src/BiliTool.Vn.Web/BiliTool.Vn.Web.csproj --urls "http://localhost:5050" --environment Production
pause
