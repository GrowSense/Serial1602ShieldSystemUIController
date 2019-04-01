sh inject-version.sh "1-0-0-1"

SLN_PROJ_DIR="src/Serial1602ShieldSystemUIController"

if [ -f $SLN_PROJ_DIR/app.config.default ]; then
  mv $SLN_PROJ_DIR/app.config $SLN_PROJ_DIR/app.config.security
  mv $SLN_PROJ_DIR/app.config.default $SLN_PROJ_DIR/app.config
fi
