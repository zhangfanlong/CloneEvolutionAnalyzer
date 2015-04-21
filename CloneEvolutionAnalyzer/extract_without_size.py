#!/usr/bin/python
# -*- coding: cp936 -*-
import re, sys, os
import xml.dom.minidom
import math

#C:\Users\founder\extract_without_size.py E:\jEdit-results\CRDFiles\blocks\ E:\jEdit-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_size.py E:\dnsjava-optimal-results\CRDFiles\blocks\ E:\dnsjava-optimal-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_size.py E:\wget-results\CRDFiles\blocks\ E:\wget-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_size.py E:\itextsharp-results\CRDFiles\blocks\ E:\itextsharp-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_size.py E:\conky-results\CRDFiles\blocks\ E:\conky-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_size.py E:\processhacker-results\CRDFiles\blocks\ E:\processhacker-results\MAPFiles\blocks\
nclones = []

DBT = []
SIM = []
NOP = []
CTX = []
AGE = []
CC  = []
TG  = []
file_list1 = os.listdir(sys.argv[1])
def num_of_clones(a, b):
        file_list = file_list1[a:b]
        num = 0
        for filename in file_list:
                doc = xml.dom.minidom.parse(sys.argv[1] + filename) 
                root = doc.documentElement
                class_nodes = root.getElementsByTagName('class')
                for classNode in class_nodes:
                        num = num + int(classNode.getAttribute('nclones'))
        return num
for filename1 in file_list1:
	doc1 = xml.dom.minidom.parse(sys.argv[1] + filename1) 
	root1 = doc1.documentElement
	source_nodes = root1.getElementsByTagName('source')
	for sourceNode in source_nodes:
		if sourceNode.getElementsByTagName('methodInfo') == []:
                        NOP.append('0')
		else:
                        NOP.append(sourceNode.getElementsByTagName('methodInfo')[0].getAttribute('mParaNum'))
		context = 'DEF'
		flag = 1 if sourceNode.getElementsByTagName('blockInfo') == [] else 0
		if flag == 0:
			btype_nodes = sourceNode.getElementsByTagName('bType')
			l = len(btype_nodes)
			btype_real = btype_nodes[l - 1]
			context = btype_real.childNodes[0].nodeValue
		CTX.append(context)
	class_nodes = root1.getElementsByTagName('class')
	num = 0
	for classNode in class_nodes:
		n = int(classNode.getAttribute('nclones'))
		similarity = int(classNode.getAttribute('similarity'))
		similarity = similarity / 100
		source_nodes = classNode.getElementsByTagName('source')
		files = []
		for sourceNode in source_nodes:
			files.append(sourceNode.getAttribute('file'))
			same = '1' if len(set(files)) == 1 else '0'
		while n > 0:
			DBT.append(same)
			SIM.append(str(similarity))
			n = n - 1
		num = num + int(classNode.getAttribute('nclones'))
	nclones.append(num)
for i in range(1, int(nclones[0]) + 1):
	TG.append(1)
	AGE.append('1')
	CC.append('0')
file_list2 = os.listdir(sys.argv[2])
for index in range(0, len(file_list2)):
	doc2 = xml.dom.minidom.parse(sys.argv[2] + file_list2[index]) 
	root2 = doc2.documentElement
	cgmap_nodes = root2.getElementsByTagName('CGMap')
	unmappeddestcg_nodes = root2.getElementsByTagName('UnMappedDestCG')
	unmappeddestcf_nodes = []
	if unmappeddestcg_nodes != []:
		unmappeddestcf_nodes = unmappeddestcg_nodes[0].getElementsByTagName('CGInfo')
	for destCGid in range(1, int(nclones[index + 1]) + 1):
		for cgmapnode in cgmap_nodes:
			if int(cgmapnode.getAttribute('destCGid')) == destCGid:
				pattern = 1
				harmful = 1
				epnode = cgmapnode.getElementsByTagName('EvolutionPattern')[0]
				if epnode.getAttribute('INCONSISTENTCHANGE') == 'True':
					pattern = 3
				elif epnode.getAttribute('CONSISTENTCHANGE') == 'True':
					pattern = 2
					harmful = 0 
				CGid = int(cgmapnode.getAttribute('srcCGid'))
				for i in range(index - 1, -1 ,-1):
					doc = xml.dom.minidom.parse(sys.argv[2] + file_list2[i]) 
					root = doc.documentElement
					old_cgmap_nodes = root.getElementsByTagName('CGMap')
					found = 0
					for old_cgmapnode in old_cgmap_nodes:
						if int(old_cgmapnode.getAttribute('destCGid')) == CGid:
							found = 1
							old_pattern = 1
							old_epnode = old_cgmapnode.getElementsByTagName('EvolutionPattern')[0]
							if old_epnode.getAttribute('INCONSISTENTCHANGE') == 'True':
								old_pattern = 3
							elif old_epnode.getAttribute('CONSISTENTCHANGE') == 'True':
								old_pattern = 2
								harmful = 0 
							if pattern < old_pattern:
								pattern = old_pattern
							CGid = int(old_cgmapnode.getAttribute('srcCGid'))
					if found == 0:
						break
				CGid = int(cgmapnode.getAttribute('srcCGid'))
				cfmap_nodes = cgmapnode.getElementsByTagName('CFMap')
				destCGsize = int(cgmapnode.getAttribute('destCGsize'))
				for destCFid in range(1, destCGsize + 1):
					TG.append(harmful)
					ffound = 0
					for cfmapnode in cfmap_nodes:
						if int(cfmapnode.getAttribute('destCFid')) == destCFid:
							ffound = 1
							lifetime = 1
							changetime = 0
							CFid = int(cfmapnode.getAttribute('srcCFid'))
							old_CGid = CGid
							for i in range(index - 1, -1, -1):
								doc = xml.dom.minidom.parse(sys.argv[2] + file_list2[i])
								root = doc.documentElement
								old_cgmap_nodes = root.getElementsByTagName('CGMap')
								aimCGid = old_CGid
								gfound = 0
								for old_cgmapnode in old_cgmap_nodes:
									if int(old_cgmapnode.getAttribute('destCGid')) == aimCGid:
										old_cfmap_nodes = old_cgmapnode.getElementsByTagName('CFMap')
										old_CGid = int(old_cgmapnode.getAttribute('srcCGid'))
										for old_cfmapnode in old_cfmap_nodes:
											if int(old_cfmapnode.getAttribute('destCFid')) == CFid:
												gfound = 1
												lifetime = lifetime + 1
												if old_cfmapnode.getAttribute('textSim') != '1':
													changetime = changetime + 1
												CFid = int(old_cfmapnode.getAttribute('srcCFid'))
								if gfound == 0:
									break
							AGE.append(str(lifetime))
							CC.append(str(changetime))
					if ffound == 0:
						AGE.append('1')
						CC.append('0')
		for unmappeddestcfnode in unmappeddestcf_nodes:
			if int(unmappeddestcfnode.getAttribute('id')) == destCGid:
				destCGsize = int(unmappeddestcfnode.getAttribute('size'))
				for i in range(0, destCGsize):
					TG.append(1)
					AGE.append('1')
					CC.append('0')


DBT_for_svm = []
CTX_for_svm = []

AGE_for_svm = []
CC_for_svm  = []
TG_for_svm  = []
SIM_for_svm = []
NOP_for_svm = []

for item in TG:
	if item == 1:
		item = '-1'
	elif item == 0:
		item = '+1'
	TG_for_svm.append(item)

for item in DBT:
	item = ' 1:' + item
	DBT_for_svm.append(item)
for item in CTX:
	if item == 'DEF':
		item = ' 2:1 3:0 4:0 5:0'
	elif item == 'IF' or item == 'ELSE' or item == 'SWITCH':
		item = ' 2:0 3:1 4:0 5:0'
	elif item == 'FOR' or 'WHILE' or 'DO':
		item = ' 2:0 3:0 4:1 5:0'
	else:
		item = ' 2:0 3:0 4:0 5:0'
	CTX_for_svm.append(item)

for item in AGE:
	item = ' 6:' + item
	AGE_for_svm.append(item)
for item in CC:
	item = ' 7:' + item
	CC_for_svm.append(item)
for item in SIM:
        item = ' 8:' + item
        SIM_for_svm.append(item)
for item in NOP:
        item = ' 9:' + item
        NOP_for_svm.append(item)
train_file = open('train', 'w')
test_file = open('test', 'w')
zipped = zip(TG_for_svm, DBT_for_svm, CTX_for_svm,
             AGE_for_svm, CC_for_svm, SIM_for_svm, NOP_for_svm)
k = list(zipped)
num_of_trainset = num_of_clones(0, 15)
for i in range(0, num_of_trainset):
	train_file.writelines(k[i])
	train_file.write('\n')
for i in range(num_of_trainset, len(TG_for_svm)):
        test_file.writelines(k[i])
        test_file.write('\n')
train_file.close()
test_file.close()
