echo "Packaging release zip file..."

DIR=$PWD

TMP_RELEASE_FOLDER=".tmp/Serial1602ShieldSystemUIController/lib/net40/"
BIN_RELEASE_FOLDER="bin/Release"
RELEASES_FOLDER="releases"

BRANCH=$(git branch | sed -n -e 's/^\* \(.*\)/\1/p')

VERSION_POSTFIX=""

if [ "$BRANCH" != "lts" ]; then
  VERSION_POSTFIX="-$BRANCH"
fi

VERSION="$(cat version.txt).$(cat buildnumber.txt)"

mkdir -p $TMP_RELEASE_FOLDER

cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIControllerConsole.exe $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIControllerConsole.exe.config $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIControllerConsole.exe.mdb $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIController.dll $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIController.dll.mdb $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIController.Tests.dll $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIController.Tests.dll.mdb $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIController.Tests.Integration.dll $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/Serial1602ShieldSystemUIController.Tests.Integration.dll.mdb $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/duinocom.core.dll $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/nunit.framework.dll $TMP_RELEASE_FOLDER/
cp $BIN_RELEASE_FOLDER/M2Mqtt.Net.dll $TMP_RELEASE_FOLDER/

mkdir -p $RELEASES_FOLDER

cd .tmp/Serial1602ShieldSystemUIController

zip -r $DIR/releases/Serial1602ShieldSystemUIController.$VERSION$VERSION_POSTFIX.zip *

cd $DIR

rm .tmp -r

echo "Finished packaging release zip file."
