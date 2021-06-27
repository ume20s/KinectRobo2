import wiringpi as pi
import serial
import time

def main():
    S_pin1 = 19
    S_pin2 = 18
    pi.wiringPiSetupGpio()
    pi.pinMode( S_pin1, 2 )
    pi.pinMode( S_pin2, 2 )
    pi.pwmSetMode(0)
    pi.pwmSetRange(1024)
    pi.pwmSetClock(375)
    con = serial.Serial('/dev/ttyS8', 9600)
    time.sleep(2)
    print(con.portstr)
    str=con.readline()
    print(str)
    while 1:
        str=con.readline()
        ang = str.split()
        print(ang)
        pi.pwmWrite( S_pin1, int( 74 - 48 / 90 * int(ang[0]) ) )
        pi.pwmWrite( S_pin2, int( 74 + 48 / 90 * int(ang[1]) ) )
#        pi.pwmWrite( S_pin1, int( 74 + 48 / 90 * 0 ) )
#        pi.pwmWrite( S_pin2, int( 74 + 48 / 90 * 0 ) )

if __name__ == '__main__':
    main()
