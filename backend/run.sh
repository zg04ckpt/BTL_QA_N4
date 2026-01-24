docker compose down
docker rmi qa_api
docker load -i new.tar 
docker compose up -d