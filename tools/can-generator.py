#!/usr/bin/env python3
import time
import socket
import sys
import cantools
import os

# Constants for CAN-FD
CAN_RAW = 1
CAN_RAW_FD_FRAMES = 5
SOL_CAN_RAW = 101

def main(interface="vcan0"):
    try:
        # Load DBC
        script_dir = os.path.dirname(os.path.realpath(__file__))
        dbc_path = os.path.join(script_dir, 'kiosk.dbc')
        db = cantools.database.load_file(dbc_path)
        message = db.get_message_by_name('KioskTelemetry')

        s = socket.socket(socket.PF_CAN, socket.SOCK_RAW, socket.CAN_RAW)
        s.bind((interface,))
        
        # Enable CAN-FD frames
        s.setsockopt(SOL_CAN_RAW, CAN_RAW_FD_FRAMES, 1)

        print(f"Sending CAN frames on {interface}...")
        temp_c = 20.0
        while True:
            # Encode using cantools according to DBC layout
            data = message.encode({
                'BeltMotorTemp': temp_c,
                'ScannerRPM': 3460, # Will scale by 10, meaning raw is 346
                'SafetyDoor': 1,
                'BayOccupancy': 3,
                'FaultFlags': 0
            })
            
            # Pack CAN_ID (uint32), length (uint8), flags (uint8), res (uint8,uint8) and data
            can_id = message.frame_id
            can_id |= 0x80000000 # EFF flag for extended format if desired
            
            import struct
            frame = struct.pack('<IBBB', can_id, 8, 0, 0) + data + b'\x00' * (64 - len(data))
            
            s.send(frame[:72])
            
            temp_c += 2.5
            if temp_c > 140.0:
                temp_c = 20.0
                
            time.sleep(1)

    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) > 1:
        main(sys.argv[1])
    else:
        main()
