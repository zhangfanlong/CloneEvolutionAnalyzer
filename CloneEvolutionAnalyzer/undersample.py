#!/usr/bin/python
import random
f = open('.\\test', 'r')
read = f.readline()
total_num = 0
positive_num = 0
target = []
while read:
	total_num = total_num + 1
	if read[0:2] == '+1':
		positive_num = positive_num + 1	
	read = f.readline()
negtive_num = total_num - positive_num
f.close()
f = open('.\\test', 'r')
read = f.readline()
while read:
	if read[0:2] == '+1':
		target.append(read)
	else:
                if random.randint(1, negtive_num) < positive_num:
                        target.append(read)
	read = f.readline()
f.close()
f = open('undersample.test', 'w')
for x in target:
        f.write(x)
f.close()


