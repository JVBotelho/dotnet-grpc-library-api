#include "config.h"
#include <cstdlib>

KioskConfig KioskConfig::load_from_env() {
    KioskConfig config;
    
    if (const char* env_endpoint = std::getenv("KIOSK_SERVER_ENDPOINT")) {
        config.server_endpoint = env_endpoint;
    } else {
        // Assume default Kestrel HTTP endpoint on dev
        config.server_endpoint = "localhost:5000";
    }

    if (const char* env_device_id = std::getenv("KIOSK_DEVICE_ID")) {
        config.device_id = env_device_id;
    } else {
        config.device_id = "KIOSK-DEV-001";
    }

    if (const char* env_tls = std::getenv("KIOSK_USE_TLS")) {
        config.use_tls = (std::string(env_tls) == "1" || std::string(env_tls) == "true");
    } else {
        config.use_tls = true;
    }

    if (const char* env_root = std::getenv("KIOSK_ROOT_CERTS_PATH")) {
        config.root_certs_path = env_root;
    } else {
        config.root_certs_path = "certs/ca.crt";
    }

    if (const char* env_chain = std::getenv("KIOSK_CERT_CHAIN_PATH")) {
        config.cert_chain_path = env_chain;
    } else {
        config.cert_chain_path = "certs/client.crt";
    }

    if (const char* env_key = std::getenv("KIOSK_PRIVATE_KEY_PATH")) {
        config.private_key_path = env_key;
    } else {
        config.private_key_path = "certs/client.key";
    }

    if (const char* env_api_key = std::getenv("KIOSK_API_KEY")) {
        config.api_key = env_api_key;
    } else {
        config.api_key = ""; // Empty string will be rejected by server
    }

    return config;
}
