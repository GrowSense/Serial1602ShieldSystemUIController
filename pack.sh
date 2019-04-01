DIR=$PWD

TMP_DIR="_tmppack"

if [ -d $TMP_DIR ]; then
  rm $TMP_DIR -r
fi

echo "Packing a project release"

VERSION=$(cat version.txt)
BUILD_NUMBER=$(cat buildnumber.txt)

FULL_VERSION=$VERSION.$BUILD_NUMBER

BRANCH=$(git branch | sed -n -e 's/^\* \(.*\)/\1/p')

if [ "$BRANCH" = "dev" ]
then
    FULL_VERSION="$FULL_VERSION-dev"
fi

PROJECT_NAME="Serial1602ShieldSystemUIController"
FULL_PROJECT_NAME=$PROJECT_NAME.$FULL_VERSION
echo "Project name: $PROJECT_NAME"
echo "Project version: $PROJECT_NAME"

mkdir "$TMP_DIR"
mkdir "$TMP_DIR/$FULL_PROJECT_NAME"
mkdir "$TMP_DIR/$FULL_PROJECT_NAME/lib"
mkdir "$TMP_DIR/$FULL_PROJECT_NAME/lib/net40"

cp bin/Release/* $TMP_DIR/$FULL_PROJECT_NAME/lib/net40

mkdir -p releases

cd $TMP_DIR

zip ../releases/$FULL_PROJECT_NAME.zip -r $FULL_PROJECT_NAME

cd $DIR

echo "Finished packing release"
