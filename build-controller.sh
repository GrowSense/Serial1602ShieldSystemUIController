echo "Starting build"
echo "Dir: $PWD"

DIR=$PWD

MODE=$1

if [ ! "$MODE" ]; then
    MODE="Release"
fi

echo "Mode: $MODE"

cd src
sh build.sh $MODE || exit 1

cd $DIR
