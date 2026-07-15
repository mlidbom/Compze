### Documentation is co-located with source code in `_docs` folders.

See [src/Documentation-CoLocation.README.md](../../Documentation-CoLocation.README.md) for the whole pattern. The part that matters here: DocFX only processes content located under this folder, so [Ensure-CoLocatedDocsJunctions.ps1](Ensure-CoLocatedDocsJunctions.ps1) creates one git-ignored directory junction per documented project (`Compze\Teventive` → `..\..\Compze.Teventive`, ...). It runs automatically before every docfx build via the npm pre-hooks and from buildAndPublish.ps1; run it by hand after giving a new project a `_docs` folder.

This project also compiles every co-located `_docs` example file in the framework (via a `Compile Include` over `..\..\Compze.*\**\_docs\*.cs`), so the compiler catches documentation rot when the API changes.

### Local development

Look in package.json for details on what these instructions actually execute.

* For local development, run npm install to install dependencies
	* If in visual studio
		* make sure to install the NPM Task Runner extension
		* open the Task Runner Explorer and run the 'watch-site-only' task
		* once that is up, run browser-livereload or the live-server. I introduced live-server because of some issues with browser-livereload that I don't remember.

* watch-site-only task will watch for changes in the site folder and rebuild the site as needed
* browser-livereload/live-server will serve the site and update the browser when changes are made

To build and publish, run buildAndPublish.ps1
