Look in package.json for information on how to build and run this project

For local development, run `npm install` to install dependencies
If in visual studio, make sure to install the NPM Task Runner extension
then open the Task Runner Explorer and run the 'watch-site-only' task and once that is up and running the 'browser-livereload'
the watch-site-only task will watch for changes in the site folder and recompile as needed while the browser-livereload task will serve the site and update the browser when changes are made

To build and publish, run buildAndPublish.ps1