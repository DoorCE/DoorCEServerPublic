docker login
dotnet dev-certs https --export-path aspnetcore.pfx -p pass
$REV_TAG = $(git log -1 --pretty=format:%h)
docker build -f Dockerfile.Generator -t doorcegenerator:$REV_TAG --build-arg BUILD_TAG=$REV_TAG .
docker tag doorcegenerator:$REV_TAG doorce/doorcegenerator:$REV_TAG
docker push doorce/doorcegenerator:$REV_TAG