@echo off
setlocal

:: If no file was provided, ask for one
if "%~1"=="" (
    echo Drag a video file onto this .bat, or run:
    echo    unity_encode.bat inputfile
    pause
    exit /b
)

set "infile=%~1"
set "base=%~n1"
set "outfile=C:\Users\ehickey\Documents\Unity\spacefight\Assets\Videos\%base%_unity.mp4"

echo Input:  %infile%
echo Output: %outfile%
echo.

ffmpeg -fflags +genpts -i "%infile%" ^
 -c:v libx264 -profile:v baseline -level 3.0 ^
 -x264-params "bframes=0:ref=1:force-cfr=1" ^
 -vf "setpts=PTS-STARTPTS" ^
 -pix_fmt yuv420p ^
 -fps_mode cfr ^
 -an -r 15^
 "%outfile%"


echo.
echo Done.
pause
