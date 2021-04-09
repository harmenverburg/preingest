# Example pre-wash XSLT transformations

This folder holds some example XSLT files that can be used in pre-wash transformations. The frontend will fetch the list
of all available files, ignore anything that starts with an underscore or does not end with `.xslt`, and show a sorted
list without the `.xslt` extension.

When using the [docker-compose.yml](../docker-compose.yml) file, see also the required environment variable to define
the location of the pre-wash XSLT files.
