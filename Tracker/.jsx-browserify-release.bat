@echo off

set NODE_ENV=production
set NODE_ENV
browserify -t babelify jsx\manageEvent.jsx -o Scripts\manageEvent.js