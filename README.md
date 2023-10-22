# SDMeta

* Bulk extract metadata from SD generated PNG files.
* View and search in a web UI.

# Command line usage

```
Usage:
  SDMetaTool [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  list <path>  List sd metadata to csv.
  info <path>  Info on files.
```

## Running on Windows

```ps
.\SDMetaTool.exe list \\nas\sd --outfile info.csv
```

## Running on Docker

```bash
docker volume create sdmetatool_data

docker pull ghcr.io/jamesmoore/sdmeta:main

docker run \
--name sdmetatool \
--rm \
-v /mnt/storage/sd/:/sd:ro \
-v sdmetatool_data:/var/lib/sdmetatool \
-v $(pwd):/app/export \
ghcr.io/jamesmoore/sdmeta:main list /sd -o ./export/sdmetalist.csv
```

# Web UI

## Running on Windows

```ps
.\SDMetaUI.exe --ImageDir=E:\stable-diffusion-webui\outputs
```

## Running on Docker
```bash
docker volume create sdmeta_data

docker pull ghcr.io/jamesmoore/sdmeta:main

docker run \
-d \
-v /mnt/storage/sd/:/sd \
-v sdmeta_data:/var/lib/sdmeta \
-e ImageDir='/sd' \
-p 80:80 \
--entrypoint dotnet \
--restart always \
--log-opt max-size=1m \
ghcr.io/jamesmoore/sdmeta:main \
SDMetaUI.dll
```

The ```ImageDir``` env variable points to the folder containing the generated images. The web server runs on port 80, but you can reassign it using the `-p` parameter and/or route it through a reverse proxy like Caddy.

## Data Volumes
There are two directories required - one for the database of metadata and one for the image files. 

### Image files
This is for the images generated by stable diffusion. The image files are read to extract the metadata and not modified so you can mount this as read only unless you want to use the UI to delete images.

### Database and thumbnails
This volume is needed to hold the Sqlite database that caches the metadata and any thumbnails generated by the UI. On linux systems this volume should be mounted at ```/var/lib/sdmeta```. On Windows the location will be ```$env:APPDATA\SDMeta```

