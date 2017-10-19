# Sitefinity Shell commands

## Getting around

- Typing `help` will display the commands available for the current section
- The prompt indicates the section you are in (pages, backend pages, errors, documents, etc.)
- Press Ctrl-L to clear the screen
- Use the up and down arrow to go through the command history
- Press the tab key for completion (IDs only). Note that you first need to type `list` beforehand for the completion to work
- Type `site` to see the list of the sites, as well as the currently selected site
- Type `site <site ID>` to switch to another site
- Commands are case-insensitive

## Pages / Backend pages

`pages` switches to the Frontend pages section, whereas `bpages` switches to the Backend pages section. Note that less commands are available for the latter than the former due to their impact on the site administration.

`list` displays the pages in the current "tree", whereas `list all` recursively displays the pages and their children. Use the `cd <ID>`, `cd ..` or `cd` commands to respectively go down a subtree, move up or move to the top level.

The `list` output can be redirected to another command separated by a comma. For instance, `list all, filter requiressl=true, republish` will recursively list all the pages in the current subtree, send this list to the `filter` command that will keep only the pages whose RequireSSL flag is set to true. The resulting pages will be sent to the `republish` command and will be republished. Example of chains of commands are `<list command>`, `<filter command>`, `<modification command>` or `<list command>`, `<filter command>`, `display <fields to display>`

Modification commands:

- `update` allows to modify a field, whether RequireSSL, cache or template. `update nbversions=<nb>` removes older versions for pages which have more than <nb> versions.
- `republish` and `touch` modify the last modify date of a page without performing actual changes, the former command performing a full republish and thus creating a new version.

The `display` command sets the fields being displayed: id, requireSSL, cache, template, permissions. `permissions` is not a field per say but displays the "Permission Group" of a page if it doesn't inherits permissions from its parent. Permission Groups are arbitrary numbers that describe a complete permission set. Two pages in the same Permission Group will have the exact same permission settings, i.e. all the users and roles will have the exact same permissions. This allows to see if some pages have permissions set slightly differently than other pages.

## Documents / Images / Videos

`docs`, `images` and `videos` switch to respectively the Documents, Images and Videos section.

The `list`, `cd` and `republish` commands work the same way as for pages. `update nbversions=<nb>`, like for pages, removes older versions for media content which have more than <nb> versions.

## Errors

`errors` switch to the Error section, allowing to examine the Error*.log files in ~/App_Data/Sitefinity/Logs/ even if Sitefinity holds a lock on Error.log.

`summary` goes through all the Error log files and groups them by message ("Message:" line in the error logs), displaying the number of occurrences by ascending order. This allows to see the errors the most commonly hit. `summary url` groups errors by URL whereas `summary stack` groups errors by the top stack trace line.

`list` looks at the errors from Error.log whereas `list all` looks in all the Error*.log files. Like for pages, the output of a `list` command can be sent to a `filter` command, e.g. `list all, filter message=file does not`. The value entered in the filter is case-insensitive, does not need to be complete and can contain spaces. It can also contain multiple filters, e.g. `filter message=file does not url=appstatus` will filter errors whose Message contains "file does not" and whose URL contains "appstatus".

Like for pages, `display` allows to display only the desired fields: timestamp, message, url, stack (top line of the call stack) or fullstack (full stack trace)

## SiteSync

`sitesync` switches to the SiteSync section, allowing to examine the Synchronization*.log files. This section will only work if 1) the SiteSync module is unabled and 2) SiteSync is either configured as a Source (i.e. a Destination server is entered in the Settings) and/or as a Destination (the "Allow content from other sites to be published to this site" option is checked in the Settings)

`list` looks for errors in the synchronization logs. It automatically tries to figure out whether the site is configured as a Source or Destination server and will read the Synchronization logs accordingly. If the site is configured as both, `list` will require to pass either `src` or `dst` to tell whether to read the content sent from the Source or Destination's point of view.

`compare` compares the Synchronization logs from both Source and Destination servers, displaying errors and any discrepancy. This command needs to be run on the Source server, and will automatically read the credentials to talk to the Destination server(s) from the SiteSync configuration. The Destination server(s) need to be up and running and have the Sitefinity Shell installed for `compare` to work.

`call` has Sitefinity contact a URL and detect any potential issue (invalid hostname, invalid SSL certificate, redirection, TLS version not supported, etc)

- `call http://www.google.com`: tests whether Sitefinity can successfully call http://www.google.com
- `call target`: tests whether Sitefinity can successfully call all the SiteSync Servers defined in Administration / Settings / SiteSync
- `call nlb`: tests whether Sitefinity can successfully call all the Load Balancing nodes defined in Adminstration / Settings / Advanced / System / LoadBalancing / WebServerUrls

Both commands contains a `detail` argument which displays more details about any error found.

## Audit Trail

`audit` switches to the Audit Trail section, allowing to see and filter audit events using the `list`, `filter` and `display` commands.

# All

`all` switches to the All section, which allows to republish all the content of a given site: pages, pages templates, news items, blogs and blog posts, calendars and events, libraries, documents, images, videos, lists and list items, forms, shared content blocks, taxonomies, dynamic content.

Type `list` to list all the sites, and `republish <site ID> <content type>` to republish all the content available for a given site. The content type can either be 'all' (to republish everything) or one of the content types supported (see help for a list)