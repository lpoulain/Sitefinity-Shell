# Sitefinity Shell

### WARNING: This is provided AS-IS and is NOT an official Sitefinity package.

![](sitefinity_shell.png)

Sitefinity Shell is a backend command-line tool to help improve some administrative tasks like:

- Find pages with certain characteristics - those with the Require SSL field, a certain template or cache setting
- Modify some pages for a particular subtree: change the Require SSL flag, the cache settings, or trim older versions
- Republish some parts of a site - a page subtree, only the pages with a particular, all the images... or all the content of your site (including Dynamic Modules)
- Examine the error logs from Sitefinity even if the process holds a lock on them
- Determine what errors are the most common, or what URLs generate the most errors
- Examine the SiteSync logs, including a full comparison between what the Source and the Destination site logs

## Documentation

Check [here](documentation.md)

## How to install

Installing the Sitefinity Shell requires to do the following:

- Build the SitefinityLogs project, which will generate `SitefinityLogs.dll`
- Copy the files inside `SitefinityWebApp` to your Sitefinity project
- Add a dependency to `SitefinityLogs.dll` to your Sitefinity project
- Rebuild
- Add the "Sitefinity Shell" widget to any backend page

I am working on a better way to integrate the Shell to a Sitefinity project, but that may take some time.

## Command-Line Tools

Two parts of the Sitefinity Shell are available as Windows command-line tools: `SFErrorLogs.exe` and `SFSiteSyncLogs.exe` (both requiring SitefinityLogs.dll). Those utilities have the same functionalities as the `errors` and `sitesync` features: examining the error logs and the SiteSync logs.
