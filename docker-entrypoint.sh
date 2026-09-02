#!/bin/sh
set -e

# Bind-mounted host directories are usually owned by whatever user created them
# on the host, not by APP_UID inside the container, so the app user can't write
# to /data until we fix that up here (while still root, before dropping privileges).
chown "$APP_UID":"$APP_UID" /data

exec gosu "$APP_UID" "$@"
