#!/bin/bash

SCALAR_PKG=$1
if [ -z "${SCALAR_PKG}" ]; then
  echo "Error: Scalar installer package not specified"
  exit 1
fi

GIT_PKG=$2
if [ -z "${GIT_PKG}" ]; then
  echo "Error: Git installer package not specified"
  exit 1
fi

GCMCORE_URL=$3
if [ -z "${GCMCORE_URL}" ]; then
  echo "Error: GCM Core package URL"
  exit 1
fi

SCRIPT_TEMPLATE=$4
if [ -z "${SCRIPT_TEMPLATE}" ]; then
  echo "Error: Distribution script template not specified"
  exit 1
fi

OUT_DIR=$5
if [ -z "${OUT_DIR}" ]; then
  echo "Error: Output directory not specified"
  exit 1
fi

function CopyScalar()
{
    SCALAR_DESTINATION="${OUT_DIR}/Scalar"

    if [ ! -f "${SCALAR_PKG}" ] ; then
        echo "Error: Could not find Scalar for Mac package at ${SCALAR_PKG}."
        exit 1
    fi

    mkdir -p "${SCALAR_DESTINATION}" || exit 1
    cp -Rf "${SCALAR_PKG}" "${SCALAR_DESTINATION}" || exit 1
}

function CopyGit()
{
    GIT_DESTINATION="${OUT_DIR}/Git"

    if [ ! -f "${GIT_PKG}" ] ; then
        echo "Error: Could not find Git for Mac package at ${GIT_PKG}."
        exit 1
    fi

    mkdir -p "${GIT_DESTINATION}" || exit 1
    cp -Rf "${GIT_PKG}" "${GIT_DESTINATION}" || exit 1
}

function CopyGcmCore()
{
    GCMCORE_FILENAME=${GCMCORE_URL##*/}
    GCMCORE_DESTINATION="${OUT_DIR}/GCM"

    mkdir -p "${GCMCORE_DESTINATION}" || exit 1
    curl -L -o "${GCMCORE_DESTINATION}/${GCMCORE_FILENAME}" "${GCMCORE_URL}" || exit 1
}

function UpdateScript()
{
    GIT_FILENAME=$(basename ${GIT_PKG})
    SCALAR_FILENAME=$(basename ${SCALAR_PKG})

    /usr/bin/sed -e "s|##GIT_INSTALLER_PKG_PLACEHOLDER##|${GIT_FILENAME}|g" \
                 -e "s|##GCM_CORE_INSTALLER_PKG_PLACEHOLDER##|${GCMCORE_FILENAME}|g" \
                 -e "s|##SCALAR_INSTALLER_PKG_PLACEHOLDER##|${SCALAR_FILENAME}|g" \
                 "${SCRIPT_TEMPLATE}" > "${OUT_DIR}/InstallScalar.sh" || exit 1

    /bin/chmod +x "${OUT_DIR}/InstallScalar.sh" || exit 1
}

function Run()
{
    CopyGit
    CopyGcmCore
    CopyScalar
    UpdateScript
}

Run
