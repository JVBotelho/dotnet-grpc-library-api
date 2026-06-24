#include <iostream>
#include <string>
#include <thread>
#include "config.h"
#include "KioskClient.h"
#include "CanReader.h"

void print_usage(const char* prog_name) {
    std::cout << "Usage:\n"
              << "  " << prog_name << " --validate <card_uid>\n"
              << "  " << prog_name << " --can-link <can_interface>\n"
              << std::endl;
}

int main(int argc, char** argv) {
    if (argc < 2) {
        print_usage(argv[0]);
        return 1;
    }

    std::string arg1 = argv[1];

    KioskConfig config = KioskConfig::load_from_env();
    std::cout << "Connecting to Kiosk Server at " << config.server_endpoint << " as " << config.device_id << "\n";

    KioskClient client(config);

    if (arg1 == "--validate" && argc == 3) {
        std::string card_uid = argv[2];
        client.ValidateMember(card_uid);
    } else if (arg1 == "--return" && argc == 3) {
        std::string book_id = argv[2];
        client.ConnectAndSync(); // start network watcher
        client.BulkReturn(book_id);
        
        // Let it sync if offline queue is working
        std::this_thread::sleep_for(std::chrono::seconds(2));
    } else if (arg1 == "--can-link" && argc == 3) {
        std::string can_iface = argv[2];
        
        // Start background watcher & sync queue
        client.ConnectAndSync();

        // Start listening to CAN
        CanReader reader(can_iface, config.device_id);
        reader.Start([&client](const LibrarySystem::Contracts::Protos::DeviceFrame& frame) {
            std::cout << "[CAN] Received frame ID=" << frame.can_id() << " forwarding..." << std::endl;
            client.EnqueueFrame(frame);
        });

        std::cout << "Press ENTER to stop..." << std::endl;
        std::cin.get();

        reader.Stop();
        client.StopDeviceLink();
    }
    else {
        print_usage(argv[0]);
        return 1;
    }

    return 0;
}
