import paramiko

def write_to_file(host, user, password, path, file):
	ssh = paramiko.SSHClient()
	ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
	ssh.connect(hostname=host,username=user,password=password)
	sftp = ssh.open_sftp()
	sftp.put(path, '/home/student/ErrorLogFiles/' + file)
	sftp.close()
	ssh.close()