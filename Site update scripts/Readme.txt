Use this create_update.cmd to prepare files that require update on the web server, since some revison. It considers only files that make sense on the server: *.dll in bin\ dirs, *.asmx, *.ascx, *.asax, *.config etc - but NOT .cs and alikes.

For example, to create a directory tree with files that require update:
	create_update.cmd v14_04_06
This will check update from v14_04_06 tag (non-inclusive, assuming it's the tag installed on the web server now) to HEAD, and create a folder Update in the current diectory.
