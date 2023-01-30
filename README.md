# WSyncer

A simple command-line program for syncing files between folders.

If you run it like this:

`WSyncer.exe C:/Source C:/Destination 1`

it will (only) simulate sync process from `Source` folder onto `Destination` folder.

It you call:

`WSyncer.exe C:/Source C:/Destination 0`

the program will actually perform the sync. The content of `C:/Destination` will be a "copy" of `C:/Source`.
