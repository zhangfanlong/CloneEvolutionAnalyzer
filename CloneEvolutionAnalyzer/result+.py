#!/usr/bin/python
# -*- coding: cp936 -*-
#ͳ��ʵ����

#��ѵ����
f = open('D:\\2\\train', 'r')
read = f.readline()
total_num = 0#��������
positive_num = 0#��������
while read:
	total_num = total_num + 1
	if read[0:2] == '+1':
		positive_num = positive_num + 1
	read = f.readline()
print('Result:')
print('+++++++++++++++++++++++++++++++')
print('1.# of + in train: ' + str(positive_num))
print('2.# of - in train: ' + str(total_num - positive_num))
resulttemp_file = open('result_temp', 'w')

resulttemp_file.write('1.# of + in train: ' + str(positive_num) + '\n')
resulttemp_file.write('2.# of - in train: ' + str(total_num - positive_num) + '\n')
f.close()

#�򿪲��Լ�
f = open('D:\\2\\test', 'r')
read = f.readline()
total_num = 0#��������
positive_num = 0#��������
target = []#���Լ�Ŀ������
while read:
	total_num = total_num + 1
	target.append(read[0:2])
	if read[0:2] == '+1':
		positive_num = positive_num + 1
	read = f.readline()
print('+++++++++++++++++++++++++++++++')
print('1.# of + in test: ' + str(positive_num))
print('2.# of - in test: ' + str(total_num - positive_num))
resulttemp_file.write('+++++++++++++++++++++++++++++++' + '\n')
resulttemp_file.write('1.# of + in test: ' + str(positive_num) + '\n')
resulttemp_file.write('2.# of - in test: ' + str(total_num - positive_num) + '\n')
f.close()

#��Ԥ���ļ�
f = open('D:\\2\\test.predict', 'r')
read = f.readline()
#read = f.readline() �߼��ع���ļ�ͷ��֧���������Ĳ�һ������Ҫ���һ��
positive_num_predict = 0#Ԥ�����������
target_predict = []#Ԥ���Ŀ������
while read:
        target_predict.append(read[0:1])
        if read[0:1] == '1':
                positive_num_predict = positive_num_predict + 1
        read = f.readline()
true_positive_num = 0#Ԥ����ȷ����������
true_negtive_num = 0#Ԥ����ȷ�ĸ�������
for i in range(0, len(target)):
	if target[i] == '+1' and target_predict[i] == '1':
                true_positive_num = true_positive_num + 1
for i in range(0, len(target)):
        if target[i] == '-1' and target_predict[i] == '-':
                true_negtive_num = true_negtive_num + 1
precision = true_positive_num / float(positive_num_predict)#��׼��
recall = true_positive_num / float(positive_num)#��ȫ��
positive_error = positive_num_predict - true_positive_num#Ԥ��������������
negtive_num_predict = total_num - positive_num_predict
negtive_error = negtive_num_predict - true_negtive_num#Ԥ�����ĸ�������
print('+++++++++++++++++++++++++++++++')
print('1.Precision: ' + str(precision))
print('2.Recall: ' + str(recall))
f_value = 2*precision*recall / float(precision + recall)
print('3.F-Value: ' + str(f_value))
print('4.# of + error: ' + str(positive_error))
print('5.# of - error: ' + str(negtive_error))
resulttemp_file.write('+++++++++++++++++++++++++++++++' + '\n')
resulttemp_file.write('1.Precision: ' + str(precision) + '\n')
resulttemp_file.write('2.Recall: ' + str(recall) + '\n')
resulttemp_file.write('3.F-Value: ' + str(f_value) + '\n')
resulttemp_file.write('4.# of + error: ' + str(positive_error) + '\n')
resulttemp_file.write('5.# of - error: ' + str(negtive_error) + '\n')

resulttemp_file.close()
f.close()

