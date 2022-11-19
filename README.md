# SDMetaTool

Bulk extract metadata from SD generated PNG files.

## Running on windows

```ps
.\SDMetaTool.exe list \\nas\sd --outfile info.csv
```

## Running on Docker

```bash
docker volume create sdmetatool_data

docker pull ghcr.io/jamesmoore/sdmetatool:main

docker run \
--name sdmetatool \
--rm \
-v /mnt/storage/sd/:/sd \
-v sdmetatool_data:/data \
-v $(pwd):/app/export \
ghcr.io/jamesmoore/sdmetatool:main list /sd -o ./export/sdmetalist.csv
```