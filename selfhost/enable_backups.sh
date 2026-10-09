#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
DIR="$(pwd)"
HOUR="${1:-4}"

echo "==> Включаю ежедневное резервное копирование в ${HOUR}:00"
cat >/etc/cron.d/site-backup <<CRON
SHELL=/bin/bash
PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
0 ${HOUR} * * * root cd ${DIR} && bash backup.sh >> /var/log/site-backup.log 2>&1
CRON
chmod 644 /etc/cron.d/site-backup

if crontab -l 2>/dev/null | grep -q "backup.sh"; then
	echo "==> Убираю старую ручную запись, чтобы копии не делались дважды"
	crontab -l | grep -v "backup.sh" | crontab -
fi

echo "==> Делаю первую копию прямо сейчас"
bash backup.sh

echo
echo "Готово. Копии будут создаваться каждый день в ${HOUR}:00 и храниться 14 дней."
echo "Журнал: /var/log/site-backup.log"
