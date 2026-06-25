#pragma once
#include <string>

struct KioskConfig {
    std::string server_endpoint;
    std::string device_id;
    bool use_tls;
    std::string root_certs_path;
    std::string cert_chain_path;
    std::string private_key_path;
    std::string api_key;
    
    static KioskConfig load_from_env();
};
