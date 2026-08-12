#!/usr/bin/env ruby
# encoding: utf-8

require 'json'
require 'optparse'

gitlab_features_file = nil
export_json_file = nil

OptionParser.new do |opts|
  opts.banner = 'Usage: scan.features.rb [options]'

  opts.on('-s', '--src-dir PATH', 'Specify gitlab source dir (required if --features-file is omitted)') do |v|
    gitlab_features_file = File.expand_path(File.join(v, 'ee/app/models/gitlab_subscriptions/features.rb'))
  end

  opts.on('-f', '--features-file PATH', 'Specify gitlab features path (required if --src-dir is omitted)') do |v|
    gitlab_features_file = File.expand_path(v)
  end

  opts.on('-o', '--output PATH', 'Output to json file (required)') do |v|
    export_json_file = File.expand_path(v)
  end

  opts.on('-h', '--help', 'Prints this help') do
    puts opts
    exit
  end
end.parse!

if gitlab_features_file.nil? || export_json_file.nil?
  puts '[!] missing required options'
  puts '[!] use -h for help'
  exit 1
end

unless File.file?(gitlab_features_file)
  puts "[!] features file not found: #{gitlab_features_file}"
  exit 1
end

puts "Reading features from #{gitlab_features_file}"

begin
  Kernel.load(gitlab_features_file)
rescue StandardError, LoadError => e
  warn "[!] failed to load GitLab features file; continuing with partial results: #{e.class}: #{e.message}"
end

all_features = []

if defined?(GitlabSubscriptions) && defined?(GitlabSubscriptions::Features)
  GitlabSubscriptions::Features.constants.each do |const_name|
    next unless const_name.to_s.include?('FEATURE')

    const_value = GitlabSubscriptions::Features.const_get(const_name)
    if const_value.respond_to?(:to_ary)
      all_features.concat(const_value.to_a)
    else
      all_features << const_value
    end
  end
else
  warn '[!] GitlabSubscriptions::Features was not loaded from the features file; continuing with an empty set.'
end

all_features.uniq!
all_features.sort_by! { |feature| feature.to_s }

puts "[*] total features: #{all_features.size}"
puts "[*] writing to #{export_json_file}"
File.write(export_json_file, JSON.pretty_generate(all_features))
puts '[*] done'
