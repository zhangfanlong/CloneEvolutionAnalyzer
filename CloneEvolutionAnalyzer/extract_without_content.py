#!/usr/bin/python
# -*- coding: cp936 -*-
import re, sys, os
import xml.dom.minidom
import math

#C:\Users\founder\extract_without_content.py E:\jEdit-results\CRDFiles\blocks\ E:\jEdit-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_content.py E:\dnsjava-optimal-results\CRDFiles\blocks\ E:\dnsjava-optimal-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_content.py E:\wget-results\CRDFiles\blocks\ E:\wget-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_content.py E:\itextsharp-results\CRDFiles\blocks\ E:\itextsharp-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_content.py E:\conky-results\CRDFiles\blocks\ E:\conky-results\MAPFiles\blocks\
#C:\Users\founder\extract_without_content.py E:\processhacker-results\CRDFiles\blocks\ E:\processhacker-results\MAPFiles\blocks\
nclones = []
LOC = []
NOT = []
NOD = []
UOT = []
UOD = []
LEN = []
VCB = []
VOL = []
LV = []
DIF = []
CON = []
EFF = []
PT = []
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
		e = int(sourceNode.getAttribute('endline'))
		s = int(sourceNode.getAttribute('startline'))
		LOC.append(str(e - s))
		uotNode = sourceNode.getElementsByTagName('UniqueOprator')[0]
		uotvalue = int(uotNode.childNodes[0].nodeValue)
		UOT.append(str(uotvalue))
		uodNode = sourceNode.getElementsByTagName('UniqueOprand')[0]
		uodvalue = int(uodNode.childNodes[0].nodeValue)
		UOD.append(str(uodvalue))
		notNode = sourceNode.getElementsByTagName('TotalOprator')[0]
		notvalue = int(notNode.childNodes[0].nodeValue)
		NOT.append(str(notvalue))
		nodNode = sourceNode.getElementsByTagName('TotalOprand')[0]
		nodvalue = int(nodNode.childNodes[0].nodeValue)
		NOD.append(str(nodvalue))
		N = notvalue + nodvalue
		C = uotvalue + uodvalue
		V = N * math.log(float(C), 2)
		L = (2 * uodvalue) / float(uotvalue * nodvalue)
		D = 1 / float(L)
		I = L * V
		E = V * D
		T = float(E) / 18
		LEN.append(str(N))
		VCB.append(str(C))
		VOL.append(str(V))
		LV.append(str(L))
		DIF.append(str(D))
		CON.append(str(I))
		EFF.append(str(E))
		PT.append(str(T))
	class_nodes = root1.getElementsByTagName('class')
	num = 0
	for classNode in class_nodes:
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

LOC_for_svm = []


NOT_for_svm = []
NOD_for_svm = []
UOT_for_svm = []
UOD_for_svm = []
EP_for_svm  = []
AGE_for_svm = []
CC_for_svm  = []
TG_for_svm  = []


LEN_for_svm = []
VCB_for_svm = []
VOL_for_svm = []
LV_for_svm = []
DIF_for_svm = []
CON_for_svm = []
EFF_for_svm = []
PT_for_svm = []
for item in TG:
	if item == 1:
		item = '-1'
	elif item == 0:
		item = '+1'
	TG_for_svm.append(item)
for item in LOC:
	item = ' 1:' + item
	LOC_for_svm.append(item)
for item in NOT:
	item = ' 2:' + item
	NOT_for_svm.append(item)
for item in NOD:
	item = ' 3:' + item
	NOD_for_svm.append(item)
for item in UOT:
	item = ' 4:' + item
	UOT_for_svm.append(item)
for item in UOD:
	item = ' 5:' + item
	UOD_for_svm.append(item)
for item in AGE:
	item = ' 6:' + item
	AGE_for_svm.append(item)
for item in CC:
	item = ' 7:' + item
	CC_for_svm.append(item)
for item in LEN:
        item = ' 8:' + item
        LEN_for_svm.append(item)
for item in VCB:
        item = ' 9:' + item
        VCB_for_svm.append(item)
for item in VOL:
        item = ' 10:' + item
        VOL_for_svm.append(item)
for item in LV:
        item = ' 11:' + item
        LV_for_svm.append(item)
for item in DIF:
        item = ' 12:' + item
        DIF_for_svm.append(item)
for item in CON:
        item = ' 13:' + item
        CON_for_svm.append(item)
for item in EFF:
        item = ' 14:' + item
        EFF_for_svm.append(item)
for item in PT:
        item = ' 15:' + item
        PT_for_svm.append(item)

train_file = open('train', 'w')
test_file = open('test', 'w')
zipped = zip(TG_for_svm, LOC_for_svm, NOT_for_svm, NOD_for_svm, UOT_for_svm, UOD_for_svm,
             AGE_for_svm, CC_for_svm, LEN_for_svm, VCB_for_svm, VOL_for_svm, LV_for_svm, DIF_for_svm, CON_for_svm, EFF_for_svm, PT_for_svm)
k = list(zipped)
num_of_trainset = num_of_clones(0, 15)
for i in range(0, num_of_trainset):
	train_file.writelines(k[i])
	train_file.write('\n')
for i in range(num_of_trainset, len(LOC_for_svm)):
        test_file.writelines(k[i])
        test_file.write('\n')
train_file.close()
test_file.close()
