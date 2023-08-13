@echo off

if not exist ".env/Carbon" (
	git clone "https://github.com/CarbonCommunity/Carbon.Core.git" ".env\Carbon"
)
git fetch
git pull

set home=%cd%
set carbon=.env\Carbon

cd %carbon%
call "Tools\Build\win\bootstrap.bat"

cd %carbon%
call "Tools\Build\win\build.bat"