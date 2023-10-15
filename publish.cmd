ssh tf@dbpi sudo systemctl stop pskrmqtt2db
scp -q publish\pskrmqtt2db* tf@dbpi:/home/tf
ssh tf@dbpi sudo mv /home/tf/pskrmqtt2db /opt/pskrmqtt2db/
ssh tf@dbpi sudo mv /home/tf/pskrmqtt2db.pdb /opt/pskrmqtt2db/
ssh tf@dbpi sudo systemctl start pskrmqtt2db
ssh tf@dbpi sudo chmod +x /opt/pskrmqtt2db/pskrmqtt2db