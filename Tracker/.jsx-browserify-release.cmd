@echo off

set NODE_ENV=production
echo making a single script...
call browserify.cmd -t babelify jsx\manageEvent.jsx -o Scripts\manageEvent.js
if %ERRORLEVEL% NEQ 0 goto error

echo minifying...
call uglifyjs.cmd Scripts\manageEvent.js -o Scripts\manageEvent.js -c -m
if %ERRORLEVEL% NEQ 0 goto error
echo DONE, all good.

goto finish

:error
echo.
echo ------ ERROR, see details above ------

:finish