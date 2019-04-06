sh inject-version.sh "1-0-0-1"

SLN_PROJ_DIR="src/Serial1602ShieldSystemUIController"

if [ -f $SLN_PROJ_DIR/app.config.default ]; then
  cp -f $SLN_PROJ_DIR/app.config.default $SLN_PROJ_DIR/app.config
else
  echo "Can't find default config file: $SLN_PROJ_DIR/app.config.default"
  exit 1
fi
