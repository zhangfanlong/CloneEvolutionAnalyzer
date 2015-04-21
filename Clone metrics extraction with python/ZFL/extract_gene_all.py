#!/usr/bin/python
# -*- coding: utf-8 -*-

#���ã���ȡ��¡��ϵ�Ķ���ֵ,ʹ�ô��Ⱥ����ǳ����
#ʹ�÷���:/usr/bin/python  pyname.py path1 path2
#���:���ɿ�¡��ϵ�Ķ�������д��arff�ļ���

#extract_gene.py path\GenealogyFiles\blocks\ path\gene_arff_result\

import re, sys, os
import xml.dom.minidom
import arff

#��ȡ�ļ����µ�xml�ļ�
file_list = os.listdir(sys.argv[1])
outputpath = sys.argv[2]

#��ӡ�ļ��б�

Metric = []
Matrix = []

#gb2312 -> utf-8
def convert_ecoding(file):
	f=open(file,'r').read()
	f=f.replace('<?xml version="1.0" encoding="gb2312"?>','<?xml version="1.0" encoding="utf-8"?>')
	f=unicode(f,encoding='gb2312').encode('utf-8')
	return f

def mkdir(path):
   
    # ȥ����λ�ո�
    path=path.strip()
    # ȥ��β�� \ ����
    path=path.rstrip("\\")
 
    # �ж�·���Ƿ����
    # ����     True
    # ������   False
    isExists=os.path.exists(path)
 
    # �жϽ��
    if not isExists:
        # ����������򴴽�Ŀ¼
        #print path+' �����ɹ�'
        # ����Ŀ¼��������
        os.makedirs(path)
        return True
    else:
        # ���Ŀ¼�����򲻴���������ʾĿ¼�Ѵ���
        #print path+' Ŀ¼�Ѵ���'
        return False
	
#��ȡĿ¼��ÿһ����¡��ϵ�Ķ���ֵ
for filename in file_list:
	#����ת��
	f=convert_ecoding(sys.argv[1] + filename)
	
	genedoc = xml.dom.minidom.parseString(f) 
	generoot = genedoc.documentElement
	gene_info_nodes = generoot.getElementsByTagName('GenealogyInfo')	
	if len(gene_info_nodes) != 0:
		gene_info_node=gene_info_nodes[0]
		#id
		id=int(gene_info_node.getAttribute('id'))
		#start
		#start=str(gene_info_node.getAttribute('startversion'))
		#end
		#end=str(gene_info_node.getAttribute('endversion'))
		#age
		age=int(gene_info_node.getAttribute('age'))
		
		evolu_pattern=generoot.getElementsByTagName('EvolutionPatternCount')[0] 
		#Static
		static=int(evolu_pattern.getAttribute('STATIC'))
		#same
		same=int(evolu_pattern.getAttribute('SAME'))
		#add
		add=int(evolu_pattern.getAttribute('ADD'))
		#delete
		delete=int(evolu_pattern.getAttribute('DELETE'))
		#consistent
		consistent=int(evolu_pattern.getAttribute('CONSISTENTCHANGE'))
		#inconsistent
		inconsistent=int(evolu_pattern.getAttribute('INCONSISTENTCHANGE'))
		#split
		split=int(evolu_pattern.getAttribute('SPLIT'))
		
		#���ɿ�¡��ϵ�Ķ������� 
		#Metric=[id,start,end,age,static,same,add,delete,consistent,inconsistent,split]
		#������޷�����str���ͣ�ɾ����ʼ�汾�ͽ����汾��Ϣ
		Metric=[id,age,static,same,add,delete,consistent,inconsistent,split]
		#��ÿһ������������ӵ���������
		Matrix.append(Metric)
	#else:
	#	continue
	#	single_info_node=generoot.getElementsByTagName('SingleCgGenealogy')	
	
#��Matricд���ļ���,Ĭ��GenealogyFiles\
#arff.dump(outputpath + 'clone_geneaology_result.arff', Matrix, relation="clone_geneaology_matrics", names=['id', 'start', 'end', 'age', 'Static', 'same', 'add', 'delete', 'consistent' ,'inconsistent', 'split'])
#print 'Succeed!'
	
#��Matricд���ļ���,Ĭ��GenealogyFiles\
ɾ����ʼ�汾�ͽ����汾��Ϣ
mkdir(outputpath)
arff.dump(outputpath + 'clone_geneaology_result.arff', Matrix, relation="clone_geneaology_matrics", names=['id', 'age', 'Static', 'same', 'add', 'delete', 'consistent' ,'inconsistent', 'split'])
print 'Succeed!'