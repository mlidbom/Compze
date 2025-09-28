Look in package.json for details on what these instructions actually execute.

* For local development, run npm install to install dependencies
	* If in visual studio
		* make sure to install the NPM Task Runner extension
		* open the Task Runner Explorer and run the 'watch-site-only' task 
		* once that is up, run browser-livereload or the live-server. I introduced live-server because of some issues with browser-livereload that I don't remember.

* watch-site-only task will watch for changes in the site folder and rebuild the site as needed
*  browser-livereload/live-server will serve the site and update the browser when changes are made

To build and publish, run buildAndPublish.ps1