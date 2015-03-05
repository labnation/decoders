#!/bin/bash
set -e
set -x

if [[ -z "$1" ]]; then
  echo Usage:
  echo 
  echo "$0 <Platform> [Clean]"
  echo 
  echo "Where <Platform> can be Android|MacOS|Windows|iOS"
  echo "When the second argument is Clean, the working tree is cleaned up"
  exit -1
fi

#############################
# SETUP
#############################
# On mac we can't call the .exe's straight away
SOLUTION=Decoders
HOST=`uname`
if [[ x$HOST != xMINGW* ]]; then
  CMD_PREFIX="mono ./"
else
  CMD_PREFIX="./"
fi

###########################
# The general build target
###########################

TARGET=Rebuild
# The solution configurationt to build
CONFIGURATION=Release
# Where the build will reside
if [[ x$1 == xiOS ]]; then
  PLATFORM=iPhone
else
  PLATFORM=AnyCPU
fi

if [[ x$1 == xWindows ]]; then
  echo Cleaning out installer folder
  /c/Windows/Microsoft.NET/Framework/v4.0.30319/MSBuild.exe //t:${TARGET} //p:Configuration=${CONFIGURATION} ${SOLUTION}.$1.sln
elif [[ x$1 == xMacOS ]]; then
  if [[ ${CLEANUP} ]]; then
    /Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool build ${SOLUTION}.$1.sln -c:${CONFIGURATION} -t:Clean
  fi
  /Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool build ${SOLUTION}.$1.sln -c:${CONFIGURATION} -t:Build
elif [[ x$1 == xLinux ]]; then
  if [[ ${CLEANUP} ]]; then
    xbuild ${SOLUTION}.$1.sln /t:Clean /p:Configuration=${CONFIGURATION} 
  fi
  xbuild ${SOLUTION}.$1.sln /t:Build /p:Configuration=${CONFIGURATION} 
elif [[ x$1 == xAndroid ]]; then
  if [[ ${CLEANUP} ]]; then
    xbuild ${SOLUTION}.$1.sln /t:Clean
  fi
  echo Building
  xbuild ${SOLUTION}.$1.sln /p:Configuration=${CONFIGURATION} /t:${TARGET}
else
  echo Unknown platform $1
  exit -1;
fi

