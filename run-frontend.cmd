@echo off
REM ---------------------------------------------------------------------------
REM Starts the React/Vite frontend of the Knowledge Base project.
REM This file lives in the .NET API folder, so you never have to "cd" yourself.
REM
REM   .\run-frontend.cmd          -> npm run dev    (http://localhost:5173)
REM   .\run-frontend.cmd build    -> npm run build
REM ---------------------------------------------------------------------------
setlocal
pushd "%~dp0..\kb-auth-frontend" || (
  echo [ERROR] Frontend folder not found next to this script:
  echo         "%~dp0..\kb-auth-frontend"
  exit /b 1
)

echo [frontend] %CD%
if "%~1"=="" (
  call npm run dev
) else (
  call npm run %*
)

popd
endlocal
