# Description
Tests IPs by either building TCP connections or using the ICMP protocol.
# How to use
1. Enter IP in the first field of "Set Up" and mark the number to be replaced with an 'X' (e.g. "192.168.172.X")

2. Select a custom port by adding ":" and the port. (e.g. "192.168.172.X:123"). Leave blank for default port "80".

3. In the next two fields ('Begin' and 'End' give the first number and the last one to replace the "X". (e.g. for 192.168.172.10 - 192.168.172.15: { 'IP Address': "192.168.172.X", 'Begin': "10", 'End': "15" }).

4. In the field 'Threads' give the amount of threads to be used during the process. Threads allow more IPs to be tested simultaneously thus increasing testing speed but on the other hand using more network/cpu resources.

5. The timeout specifies how long each thread will wait for a response from the other device until it's IP gets marked as invalid.

6. Get the results by opening the automatically created folder IP Scanner in the directory of the executable (IPScanner.exe).
