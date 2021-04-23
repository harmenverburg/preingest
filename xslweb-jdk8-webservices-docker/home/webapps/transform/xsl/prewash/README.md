XSLWeb expects all its XSLT files in (subfolders) of a specific folder, being the parent folder
of this very one. When using [docker-compose.yml](../../../../../../docker-compose.yml) this very
folder is mapped to a volume that holds the available XSLT "pre-wash" transformations.

See also [the examples](../../../../../../prewash).