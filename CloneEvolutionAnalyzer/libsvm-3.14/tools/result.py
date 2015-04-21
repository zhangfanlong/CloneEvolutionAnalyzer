#!/usr/bin/python

f = open('.\\train', 'r')
read = f.readline()
total_num = 0
positive_num = 0
target = []
while read:
	total_num = total_num + 1
	target.append(read[0:2])
	if read[0:2] == '+1':
		positive_num = positive_num + 1
	read = f.readline()
print('Result:')
print('+++++++++++++++++++++++++++++++')
print('1.# of + in train: ' + str(positive_num))
print('2.# of - in train: ' + str(total_num - positive_num))
f.close()

f = open('.\\test', 'r')
read = f.readline()
total_num = 0
positive_num = 0
target = []
while read:
	total_num = total_num + 1
	target.append(read[0:2])
	if read[0:2] == '+1':
		positive_num = positive_num + 1
	read = f.readline()
print('+++++++++++++++++++++++++++++++')
print('1.# of + in test: ' + str(positive_num))
print('2.# of - in test: ' + str(total_num - positive_num))
f.close()

f = open('.\\test.predict', 'r')
read = f.readline()
#read = f.readline()
positive_num_predict = 0
target_predict = []
while read:
        target_predict.append(read[0:1])
        if read[0:1] == '1':
                positive_num_predict = positive_num_predict + 1
        read = f.readline()
true_positive_num = 0
true_negtive_num = 0
for i in range(0, len(target)):
	if target[i] == '+1' and target_predict[i] == '1':
                true_positive_num = true_positive_num + 1
precision = true_positive_num / float(positive_num_predict)
recall = true_positive_num / float(positive_num)

print('+++++++++++++++++++++++++++++++')
print('1.Precision: ' + str(precision))
print('2.Recall: ' + str(recall))
f_value = 2*precision*recall / float(precision + recall)
print('3.F-Value: ' + str(f_value))

f.close()

