docker login
dotnet dev-certs https --export-path aspnetcore.pfx -p pass
$REV_TAG = $(git log -1 --pretty=format:%h)
docker build -f Dockerfile.Server -t doorceserver:$REV_TAG --build-arg BUILD_TAG=$REV_TAG . --progress=plain --no-cache
docker tag doorceserver:$REV_TAG doorce/doorceserver:$REV_TAG
docker push doorce/doorceserver:$REV_TAG