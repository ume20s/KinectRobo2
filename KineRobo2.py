import Adafruit_PCA9685
import serial
import time

def main():
    R1 = 0
    R2 = 1
    L1 = 2
    L2 = 3
    pwm = Adafruit_PCA9685.PCA9685()
    pwm.set_pwm_freq(60)
    con = serial.Serial('/dev/ttyS8', 9600)
    time.sleep(2)
    print(con.portstr)
    str=con.readline()
    print(str)
    while 1:
        str=con.readline()
        ang = str.split()
        print(ang)
        pwm.set_pwm( R1, 0, int( 150 + 450 / 180 * int(ang[0]) ) )
        pwm.set_pwm( R2, 0, int( 150 + 450 / 180 * int(ang[1]) ) )
        pwm.set_pwm( L1, 0, int( 600 - 450 / 180 * int(ang[2]) ) )
        pwm.set_pwm( L2, 0, int( 600 - 450 / 180 * int(ang[3]) ) )

if __name__ == '__main__':
    main()
