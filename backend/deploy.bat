@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

rem --- Configuration ---
set "SERVER_USER=root"
set "SERVER_HOST=hoangcn.com"
set "SERVER_PATH=/hoangcn/qa_api"
set "COMPOSE_FILE=docker-compose.yaml"

echo 1. remove old image
docker rmi qa_api

echo 2. build new image 
docker build -t qa_api .
if %ERRORLEVEL% neq 0 (
    echo Docker compose build failed.
    pause
    exit /b 1
)

echo 3. saving .tar
set "IMAGE=qa_api:latest"
set "TAR=new.tar"

docker save -o "%TAR%" "%IMAGE%"
if %ERRORLEVEL% neq 0 (
    echo ERROR: failed to save %IMAGE%
    echo Listing recent images for debugging:
    docker images --format "table {{.Repository}}:{{.Tag}}\t{{.ID}}\t{{.Size}}"
    pause
    exit /b 1
)

echo 4. upload tar
scp "%TAR%" %SERVER_USER%@%SERVER_HOST%:%SERVER_PATH%/ || (
    echo SCP of tar failed & pause & exit /b 1
)

echo 5. ssh and run
ssh %SERVER_USER%@%SERVER_HOST% "cd %SERVER_PATH% && ./run.sh"

echo DONE
pause
