#!/usr/bin/env bash
set -e

# Change to the directory of the script, then go up to root
cd "$(dirname "$0")/.."

mkdir -p certs
cd certs

echo "Generating Development Root CA..."
openssl genrsa -out ca.key 2048
openssl req -x509 -new -nodes -key ca.key -sha256 -days 3650 -out ca.crt -subj "/CN=LibrarySystem Dev CA"

echo "Generating Server Certificate (localhost)..."
openssl genrsa -out server.key 2048
openssl req -new -key server.key -out server.csr -subj "/CN=localhost"
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out server.crt -days 365 -sha256 -extfile <(printf "subjectAltName=DNS:localhost,IP:127.0.0.1")

echo "Generating Server PFX for Kestrel..."
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt -certfile ca.crt -passout pass:password

echo "Generating Client Certificate for Edge Device (KIOSK-001)..."
openssl genrsa -out client.key 2048
openssl req -new -key client.key -out client.csr -subj "/CN=KIOSK-001"
openssl x509 -req -in client.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out client.crt -days 365 -sha256 -extfile <(printf "extendedKeyUsage=clientAuth\nsubjectAltName=DNS:KIOSK-001")

echo "Generating Client Certificate for Inspector WPF (INSPECTOR-WPF)..."
openssl genrsa -out inspector.key 2048
openssl req -new -key inspector.key -out inspector.csr -subj "/CN=INSPECTOR-WPF"
openssl x509 -req -in inspector.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out inspector.crt -days 365 -sha256 -extfile <(printf "extendedKeyUsage=clientAuth\nsubjectAltName=DNS:INSPECTOR-WPF")
openssl pkcs12 -export -out inspector.pfx -inkey inspector.key -in inspector.crt -certfile ca.crt -passout pass:password

echo "Done! Certificates placed in ./certs/"
