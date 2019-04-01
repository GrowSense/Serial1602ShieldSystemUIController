
export GITHUB_TOKEN=$GHTOKEN

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
echo "Project version: $FULL_VERSION"


hub release create -a releases/$FULL_PROJECT_NAME.zip -m "$FULL_PROJECT_NAME" -c v$FULL_VERSION $FULL_VERSION

