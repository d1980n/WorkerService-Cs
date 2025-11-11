## Detail Progrma:
* RMQ.cs 
* MongoDB.cs
* Worker.cs
-- perhatikan 

## Note Setting service --> systemd :
1. sudo nano /etc/systemd/system/hioto.service
2. isi file:

[Unit]
Description=Hioto Go App

After=network.target

[Service]
User=orangepi

WorkingDirectory=/home/orangepi/hioto

ExecStart=/home/orangepi/hioto/main

Restart=always

Environment=PORT=8000

[install]
WantedBy=multi-user.target

3. sudo systemctl daemon-reload
4. sudo systemctl enable hioto.service
5. sudo systemctl start hioto.service
6. sudo systemctl status hioto.service
7. sudo journalctl -fu hioto.service

